// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using osu.Framework.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Callbacks;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackBass : Track, IBassAudio
    {
        public const int BYTES_PER_SAMPLE = 4;

        private AsyncBufferStream dataStream;

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

        private FileCallbacks fileCallbacks;
        private SyncCallback endMixtimeCallback;
        private SyncCallback stopCallback;
        private SyncCallback endCallback;

        private volatile bool isLoaded;

        public override bool IsLoaded => isLoaded;

        private readonly BassRelativeFrequencyHandler relativeFrequencyHandler;

        /// <summary>
        /// Constructs a new <see cref="TrackBass"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream.</param>
        /// <param name="quick">If true, the track will not be fully loaded, and should only be used for preview purposes.  Defaults to false.</param>
        public TrackBass(Stream data, bool quick = false)
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

                    bitrate = (int)Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Bitrate);

                    stopCallback = new SyncCallback((a, b, c, d) => RaiseFailed());
                    endCallback = new SyncCallback((a, b, c, d) =>
                    {
                        if (Looping) return;

                        hasCompleted = true;
                        RaiseCompleted();
                    });
                    endMixtimeCallback = new SyncCallback((a, b, c, d) =>
                    {
                        // this is separate from the above callback as this is required to be invoked on mixtime.
                        // see "BASS_SYNC_MIXTIME" part of http://www.un4seen.com/doc/#bass/BASS_ChannelSetSync.html for reason why.
                        if (Looping)
                            seekInternal(RestartPoint);
                    });

                    Bass.ChannelSetSync(activeStream, SyncFlags.Stop, 0, stopCallback.Callback, stopCallback.Handle);
                    Bass.ChannelSetSync(activeStream, SyncFlags.End, 0, endCallback.Callback, endCallback.Handle);
                    Bass.ChannelSetSync(activeStream, SyncFlags.End | SyncFlags.Mixtime, 0, endMixtimeCallback.Callback, endMixtimeCallback.Handle);

                    isLoaded = true;

                    relativeFrequencyHandler.SetChannel(activeStream);
                    bassAmplitudeProcessor?.SetChannel(activeStream);
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
            //encapsulate incoming stream with async buffer if it isn't already.
            dataStream = data as AsyncBufferStream ?? new AsyncBufferStream(data, quick ? 8 : -1);

            fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(dataStream));

            BassFlags flags = Preview ? 0 : BassFlags.Decode | BassFlags.Prescan;
            int stream = Bass.CreateStream(StreamSystem.NoBuffer, flags, fileCallbacks.Callbacks, fileCallbacks.Handle);

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
                stream = BassFx.ReverseCreate(tempoAdjustStream, 5f, BassFlags.Default | BassFlags.FxFreeSource);

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
            Bass.ChannelSetDevice(activeStream, deviceIndex);
            BassUtils.CheckFaulted(true);

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

        private BassAmplitudeProcessor bassAmplitudeProcessor;

        protected override void UpdateState()
        {
            base.UpdateState();

            var running = isRunningState(Bass.ChannelIsActive(activeStream));
            var bytePosition = Bass.ChannelGetPosition(activeStream);

            // because device validity check isn't done frequently, when switching to "No sound" device,
            // there will be a brief time where this track will be stopped, before we resume it manually (see comments in UpdateDevice(int).)
            // this makes us appear to be playing, even if we may not be.
            isRunning = running || (isPlayed && !hasCompleted);

            Interlocked.Exchange(ref currentTime, Bass.ChannelBytes2Seconds(activeStream, bytePosition) * 1000);

            bassAmplitudeProcessor?.Update();
        }

        ~TrackBass()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (activeStream != 0)
            {
                isRunning = false;
                Bass.ChannelStop(activeStream);
                Bass.StreamFree(activeStream);
            }

            activeStream = 0;

            dataStream?.Dispose();
            dataStream = null;

            fileCallbacks?.Dispose();
            fileCallbacks = null;

            stopCallback?.Dispose();
            stopCallback = null;

            endCallback?.Dispose();
            endCallback = null;

            endMixtimeCallback?.Dispose();
            endMixtimeCallback = null;

            base.Dispose(disposing);
        }

        public override bool IsDummyDevice => false;

        public override void Stop()
        {
            base.Stop();
            StopAsync().Wait();
        }

        public Task StopAsync() => EnqueueAction(() =>
        {
            stopInternal();
            isPlayed = false;
        });

        private bool stopInternal() => isRunningState(Bass.ChannelIsActive(activeStream)) && Bass.ChannelPause(activeStream);

        private int direction;

        private void setDirection(bool reverse)
        {
            direction = reverse ? -1 : 1;
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.ReverseDirection, direction);
        }

        public override void Start()
        {
            base.Start();

            StartAsync().Wait();
        }

        public Task StartAsync() => EnqueueAction(() =>
        {
            if (startInternal())
                isPlayed = true;
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

            return Bass.ChannelPlay(activeStream);
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

        public override bool Seek(double seek) => SeekAsync(seek).Result;

        public async Task<bool> SeekAsync(double seek)
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

            if (pos != Bass.ChannelGetPosition(activeStream))
                Bass.ChannelSetPosition(activeStream, pos);
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

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(activeStream)).CurrentAmplitudes;
    }
}
