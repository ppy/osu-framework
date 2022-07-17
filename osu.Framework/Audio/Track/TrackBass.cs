// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using osu.Framework.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Callbacks;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Utils;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackBass : Track, IBassAudio, IBassAudioChannel
    {
        public const int BYTES_PER_SAMPLE = 4;

        private Stream? dataStream;

        /// <summary>
        /// Should this track only be used for preview purposes? This suggests it has not yet been fully loaded.
        /// </summary>
        public bool Preview { get; private set; }

        /// <summary>
        /// The handle for this track, if there is one.
        /// </summary>
        private int activeStream;

        /// <summary>
        /// The handle for adjusting tempo.
        /// </summary>
        private int tempoAdjustStream;

        /// <summary>
        /// This marks if the track is paused, or stopped to the end.
        /// </summary>
        private bool isPlayed;

        /// <summary>
        /// The last position that a seek will succeed for.
        /// </summary>
        private double lastSeekablePosition;

        private FileCallbacks? fileCallbacks;

        private SyncCallback? stopCallback;
        private SyncCallback? endCallback;

        private int? stopSync;
        private int? endSync;

        private volatile bool isLoaded;

        public override bool IsLoaded => isLoaded;

        private readonly BassRelativeFrequencyHandler relativeFrequencyHandler;

        /// <summary>
        /// Constructs a new <see cref="TrackBass"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream.</param>
        /// <param name="quick">If true, the track will not be fully loaded, and should only be used for preview purposes.  Defaults to false.</param>
        internal TrackBass(Stream data, bool quick = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            relativeFrequencyHandler = new BassRelativeFrequencyHandler
            {
                FrequencyChangedToZero = () => stopInternal(),
                FrequencyChangedFromZero = () =>
                {
                    // Do not resume the track if a play wasn't requested at all or has been paused via Stop().
                    if (!isPlayed) return;

                    startInternal();
                }
            };

            // todo: support this internally to match the underlying Track implementation (which can support this).
            const float tempo_minimum_supported = 0.05f;

            AggregateTempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackBass)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };

            EnqueueAction(() =>
            {
                Preview = quick;

                activeStream = prepareStream(data, quick);

                long byteLength = Bass.ChannelGetLength(activeStream);

                // will be -1 in case of an error
                double seconds = Bass.ChannelBytes2Seconds(activeStream, byteLength);

                bool success = seconds >= 0;

                if (success)
                {
                    Length = seconds * 1000;

                    // Bass does not allow seeking to the end of the track, so the last available position is 1 sample before.
                    lastSeekablePosition = Bass.ChannelBytes2Seconds(activeStream, byteLength - BYTES_PER_SAMPLE) * 1000;

                    isLoaded = true;

                    relativeFrequencyHandler.SetChannel(activeStream);
                }
            });

            InvalidateState();
        }

        private void setLoopFlag(bool value) => EnqueueAction(() =>
        {
            if (activeStream != 0)
                Bass.ChannelFlags(activeStream, value ? BassFlags.Loop : BassFlags.Default, BassFlags.Loop);
        });

        private int prepareStream(Stream data, bool quick)
        {
            switch (data)
            {
                case MemoryStream:
                case UnmanagedMemoryStream:
                case AsyncBufferStream:
                    // Buffering memory stream is definitely unworthy.
                    dataStream = data;
                    break;

                default:
                    // It would be most likely a FileStream.
                    // Consider to use RandomAccess to optimise in favor of FileStream in .NET 6
                    dataStream = new AsyncBufferStream(data, quick ? 8 : -1);
                    break;
            }

            fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(dataStream));

            BassFlags flags = (Preview ? 0 : BassFlags.Decode | BassFlags.Prescan);

            // While this shouldn't cause issues, we've had a small subset of users reporting issues on windows.
            // To keep things working let's only apply to other platforms until we know more.
            // See https://github.com/ppy/osu/issues/18652.
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                flags |= BassFlags.AsyncFile;

            int stream = Bass.CreateStream(StreamSystem.NoBuffer, flags, fileCallbacks.Callbacks, fileCallbacks.Handle);

            bitrate = (int)Math.Round(Bass.ChannelGetAttribute(stream, ChannelAttribute.Bitrate));

            if (!Preview)
            {
                // We assign the BassFlags.Decode streams to the device "bass_nodevice" to prevent them from getting
                // cleaned up during a Bass.Free call. This is necessary for seamless switching between audio devices.
                // Further, we provide the flag BassFlags.FxFreeSource such that freeing the stream also frees
                // all parent decoding streams.
                const int bass_nodevice = 0x20000;

                Bass.ChannelSetDevice(stream, bass_nodevice);
                tempoAdjustStream = BassFx.TempoCreate(stream, BassFlags.Decode | BassFlags.FxFreeSource);
                Bass.ChannelSetDevice(tempoAdjustStream, bass_nodevice);
                stream = BassFx.ReverseCreate(tempoAdjustStream, 5f, BassFlags.Default | BassFlags.FxFreeSource | BassFlags.Decode);

                Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
                Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoOverlapMilliseconds, 4);
                Bass.ChannelSetAttribute(stream, ChannelAttribute.TempoSequenceMilliseconds, 30);
            }

            return stream;
        }

        /// <summary>
        /// Returns whether the playback state is considered to be running or not.
        /// This will only return true for <see cref="PlaybackState.Playing"/> and <see cref="PlaybackState.Stalled"/>.
        /// </summary>
        private static bool isRunningState(PlaybackState state) => state == PlaybackState.Playing || state == PlaybackState.Stalled;

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            // Bass may leave us in an invalid state after the output device changes (this is true for "No sound" device)
            // if the observed state was playing before change, we should force things into a good state.
            if (isPlayed)
            {
                // While on windows, changing to "No sound" changes the playback state correctly,
                // on macOS it is left in a playing-but-stalled state. Forcefully stopping first fixes this.
                stopInternal();
                startInternal();
            }
        }

        private BassAmplitudeProcessor? bassAmplitudeProcessor;

        protected override void UpdateState()
        {
            base.UpdateState();

            bool running = isRunningState(bassMixer.ChannelIsActive(this));

            // because device validity check isn't done frequently, when switching to "No sound" device,
            // there will be a brief time where this track will be stopped, before we resume it manually (see comments in UpdateDevice(int).)
            // this makes us appear to be playing, even if we may not be.
            isRunning = running || (isPlayed && !hasCompleted);
            updateCurrentTime();

            bassAmplitudeProcessor?.Update();
        }

        public override bool IsDummyDevice => false;

        public override void Stop() => StopAsync().WaitSafely();

        public override Task StopAsync()
        {
            return EnqueueAction(() =>
            {
                stopInternal();
                isRunning = isPlayed = false;
            });
        }

        private void stopInternal()
        {
            if (!isRunningState(bassMixer.ChannelIsActive(this)))
                return;

            bassMixer.ChannelPause(this, true);
        }

        private int direction;

        private void setDirection(bool reverse)
        {
            direction = reverse ? -1 : 1;
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.ReverseDirection, direction);
        }

        public override void Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not start disposed tracks.");

            StartAsync().WaitSafely();
        }

        public override Task StartAsync() => EnqueueAction(() =>
        {
            if (startInternal())
                isRunning = isPlayed = true;
        });

        private bool startInternal()
        {
            // ensure state is correct before starting.
            InvalidateState();

            // Bass will restart the track if it has reached its end. This behavior isn't desirable so block locally.
            if (hasCompleted)
                return false;

            if (relativeFrequencyHandler.IsFrequencyZero)
                return true;

            setLoopFlag(Looping);

            return bassMixer.ChannelPlay(this);
        }

        public override bool Looping
        {
            get => base.Looping;
            set
            {
                base.Looping = value;
                setLoopFlag(Looping);
            }
        }

        public override bool Seek(double seek) => SeekAsync(seek).GetResultSafely();

        public override async Task<bool> SeekAsync(double seek)
        {
            // At this point the track may not yet be loaded which is indicated by a 0 length.
            // In that case we still want to return true, hence the conservative length.
            double conservativeLength = Length == 0 ? double.MaxValue : lastSeekablePosition;
            double conservativeClamped = Math.Clamp(seek, 0, conservativeLength);

            await EnqueueAction(() => seekInternal(seek)).ConfigureAwait(false);

            return conservativeClamped == seek;
        }

        private void seekInternal(double seek)
        {
            double clamped = Math.Clamp(seek, 0, Length);

            if (clamped < Length)
                hasCompleted = false;

            long pos = Bass.ChannelSeconds2Bytes(activeStream, clamped / 1000d);

            if (pos != bassMixer.ChannelGetPosition(this))
                bassMixer.ChannelSetPosition(this, pos);

            // current time updates are safe to perform from enqueued actions,
            // but not always safe to perform from BASS callbacks, since those can sometimes use a separate thread.
            // if it's not safe to update immediately here, the next UpdateState() call is guaranteed to update the time safely anyway.
            if (CanPerformInline)
                updateCurrentTime();
        }

        private void updateCurrentTime()
        {
            Debug.Assert(CanPerformInline);

            long bytePosition = bassMixer.ChannelGetPosition(this);
            Interlocked.Exchange(ref currentTime, Bass.ChannelBytes2Seconds(activeStream, bytePosition) * 1000);
        }

        private double currentTime;

        public override double CurrentTime => currentTime;

        private volatile bool isRunning;

        public override bool IsRunning => isRunning;

        private volatile bool hasCompleted;

        public override bool HasCompleted => hasCompleted;

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            if (activeStream == 0)
                return;

            setDirection(AggregateFrequency.Value < 0);

            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Volume, AggregateVolume.Value);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Pan, AggregateBalance.Value);
            relativeFrequencyHandler.SetFrequency(AggregateFrequency.Value);

            Bass.ChannelSetAttribute(tempoAdjustStream, ChannelAttribute.Tempo, (Math.Abs(AggregateTempo.Value) - 1) * 100);
        }

        private volatile int bitrate;

        public override int? Bitrate => bitrate;

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(this)).CurrentAmplitudes;

        private void initializeSyncs()
        {
            Debug.Assert(stopCallback == null
                         && stopSync == null
                         && endCallback == null
                         && endSync == null);

            stopCallback = new SyncCallback((_, _, _, _) => RaiseFailed());
            endCallback = new SyncCallback((_, _, _, _) =>
            {
                if (Looping)
                {
                    // because the sync callback doesn't necessarily fire in the right moment and the transition may not always be smooth,
                    // do not attempt to seek back to restart point if it is 0, and defer to the channel loop flag instead.
                    // a mixtime callback was used for this previously, but it is incompatible with mixers
                    // (as they have a playback buffer, and so a skip forward would be audible).
                    if (Precision.DefinitelyBigger(RestartPoint, 0, 1))
                        seekInternal(RestartPoint);

                    return;
                }

                hasCompleted = true;
                RaiseCompleted();
            });

            stopSync = bassMixer.ChannelSetSync(this, SyncFlags.Stop, 0, stopCallback.Callback, stopCallback.Handle);
            endSync = bassMixer.ChannelSetSync(this, SyncFlags.End, 0, endCallback.Callback, endCallback.Handle);
        }

        private void cleanUpSyncs()
        {
            if (stopSync != null) bassMixer.ChannelRemoveSync(this, stopSync.Value);
            if (endSync != null) bassMixer.ChannelRemoveSync(this, endSync.Value);

            stopSync = null;
            endSync = null;

            stopCallback?.Dispose();
            stopCallback = null;

            endCallback?.Dispose();
            endCallback = null;
        }

        #region Mixing

        protected override AudioMixer? Mixer
        {
            get => base.Mixer;
            set
            {
                // While BASS cleans up syncs automatically on mixer change, ManagedBass internally tracks the sync procedures via ChannelReferences, so clean up eagerly for safety.
                if (Mixer != null)
                    cleanUpSyncs();

                base.Mixer = value;

                // The mixer can be null in this case, if set via Dispose() / bassMixer.StreamFree(this).
                if (Mixer != null)
                {
                    // Tracks are always active until they're disposed, so they need to be added to the mix prematurely for operations like Seek() to work immediately.
                    bassMixer.AddChannelToBassMix(this);

                    // Syncs are not automatically moved on mixer change, so restore them on the new mixer manually.
                    initializeSyncs();
                }
            }
        }

        private BassAudioMixer bassMixer => (BassAudioMixer)Mixer.AsNonNull();

        bool IBassAudioChannel.IsActive => !IsDisposed;

        int IBassAudioChannel.Handle => activeStream;

        bool IBassAudioChannel.MixerChannelPaused { get; set; } = true;

        BassAudioMixer IBassAudioChannel.Mixer => bassMixer;

        #endregion

        ~TrackBass()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            cleanUpSyncs();

            if (activeStream != 0)
            {
                isRunning = false;
                bassMixer.StreamFree(this);
            }

            activeStream = 0;

            dataStream?.Dispose();
            dataStream = null;

            fileCallbacks?.Dispose();
            fileCallbacks = null;

            base.Dispose(disposing);
        }
    }
}
