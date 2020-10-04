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

        private long byteLength;

        /// <summary>
        /// The last position that a seek will succeed for.
        /// </summary>
        private double lastSeekablePosition;

        private FileCallbacks fileCallbacks;
        private SyncCallback stopCallback;
        private SyncCallback endCallback;

        private volatile bool isLoaded;

        public override bool IsLoaded => isLoaded;

        /// <summary>
        /// Constructs a new <see cref="TrackBass"/> from provided audio data.
        /// </summary>
        /// <param name="data">The sample data stream.</param>
        /// <param name="preview">If true, the track will not be fully loaded, and should only be used for preview purposes.  Defaults to false.</param>
        public TrackBass(Stream data, bool preview = false)
            : this(preview)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            EnqueueAction(() => activeStream = prepareStream(data));
            InvalidateState();
        }

        /// <summary>
        /// Constructs a new <see cref="TrackBass"/> from the provided bass stream handle.
        /// </summary>
        /// <param name="bassStreamHandle">A native bass stream handle prepared for use.</param>
        /// <param name="preview">If true, the track will not add tempo adjust capabilities, and should only be used for preview purposes. Defaults to false.</param>
        public TrackBass(int bassStreamHandle, bool preview = false)
            : this(preview)
        {
            EnqueueAction(() => activeStream = prepareStream(bassStreamHandle));
            InvalidateState();
        }

        private TrackBass(bool preview = false)
        {
            Preview = preview;

            // todo: support this internally to match the underlying Track implementation (which can support this).
            const float tempo_minimum_supported = 0.05f;

            AggregateTempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackBass)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };
        }

        private int prepareStream(Stream data)
        {
            //encapsulate incoming stream with async buffer if it isn't already.
            dataStream = data as AsyncBufferStream ?? new AsyncBufferStream(data, Preview ? 8 : -1);

            fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(dataStream));

            BassFlags flags = Preview ? 0 : BassFlags.Decode | BassFlags.Prescan;
            int stream = Bass.CreateStream(StreamSystem.NoBuffer, flags, fileCallbacks.Callbacks, fileCallbacks.Handle);

            stream = prepareStream(stream);

            return stream;
        }

        private int prepareStream(int handle)
        {
            if (!Preview)
            {
                // We assign the BassFlags.Decode streams to the device "bass_nodevice" to prevent them from getting
                // cleaned up during a Bass.Free call. This is necessary for seamless switching between audio devices.
                // Further, we provide the flag BassFlags.FxFreeSource such that freeing the stream also frees
                // all parent decoding streams.
                const int bass_nodevice = 0x20000;

                Bass.ChannelSetDevice(handle, bass_nodevice);
                tempoAdjustStream = BassFx.TempoCreate(handle, BassFlags.Decode | BassFlags.FxFreeSource);
                Bass.ChannelSetDevice(tempoAdjustStream, bass_nodevice);
                handle = BassFx.ReverseCreate(tempoAdjustStream, 5f, BassFlags.Default | BassFlags.FxFreeSource);

                Bass.ChannelSetAttribute(handle, ChannelAttribute.TempoUseQuickAlgorithm, 1);
                Bass.ChannelSetAttribute(handle, ChannelAttribute.TempoOverlapMilliseconds, 4);
                Bass.ChannelSetAttribute(handle, ChannelAttribute.TempoSequenceMilliseconds, 30);
            }

            // will be -1 in case of an error
            double seconds = Bass.ChannelBytes2Seconds(handle, byteLength = Bass.ChannelGetLength(handle));

            bool success = seconds >= 0;

            if (success)
            {
                Length = seconds * 1000;

                // Bass does not allow seeking to the end of the track, so the last available position is 1 sample before.
                lastSeekablePosition = Bass.ChannelBytes2Seconds(handle, byteLength - BYTES_PER_SAMPLE) * 1000;

                Bass.ChannelGetAttribute(handle, ChannelAttribute.Frequency, out float frequency);
                initialFrequency = frequency;
                bitrate = (int)Bass.ChannelGetAttribute(handle, ChannelAttribute.Bitrate);

                stopCallback = new SyncCallback((a, b, c, d) => RaiseFailed());
                endCallback = new SyncCallback((a, b, c, d) =>
                {
                    if (!Looping)
                        RaiseCompleted();
                });

                Bass.ChannelSetSync(handle, SyncFlags.Stop, 0, stopCallback.Callback, stopCallback.Handle);
                Bass.ChannelSetSync(handle, SyncFlags.End, 0, endCallback.Callback, endCallback.Handle);

                isLoaded = true;

                bassAmplitudeProcessor?.SetChannel(handle);
            }

            return handle;
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
            var running = isRunningState(Bass.ChannelIsActive(activeStream));
            var bytePosition = Bass.ChannelGetPosition(activeStream);

            // because device validity check isn't done frequently, when switching to "No sound" device,
            // there will be a brief time where this track will be stopped, before we resume it manually (see comments in UpdateDevice(int).)
            // this makes us appear to be playing, even if we may not be.
            isRunning = running || (isPlayed && bytePosition != byteLength);

            Interlocked.Exchange(ref currentTime, Bass.ChannelBytes2Seconds(activeStream, bytePosition) * 1000);

            bassAmplitudeProcessor?.Update();

            base.UpdateState();
        }

        protected override void Dispose(bool disposing)
        {
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
            // Bass will restart the track if it has reached its end. This behavior isn't desirable so block locally.
            if (Bass.ChannelGetPosition(activeStream) == byteLength)
                return false;

            return Bass.ChannelPlay(activeStream);
        }

        public override bool Seek(double seek) => SeekAsync(seek).Result;

        public async Task<bool> SeekAsync(double seek)
        {
            // At this point the track may not yet be loaded which is indicated by a 0 length.
            // In that case we still want to return true, hence the conservative length.
            double conservativeLength = Length == 0 ? double.MaxValue : lastSeekablePosition;
            double conservativeClamped = Math.Clamp(seek, 0, conservativeLength);

            await EnqueueAction(() =>
            {
                double clamped = Math.Clamp(seek, 0, Length);

                long pos = Bass.ChannelSeconds2Bytes(activeStream, clamped / 1000d);

                if (pos != Bass.ChannelGetPosition(activeStream))
                    Bass.ChannelSetPosition(activeStream, pos);
            });

            return conservativeClamped == seek;
        }

        private double currentTime;

        public override double CurrentTime => currentTime;

        private volatile bool isRunning;

        public override bool IsRunning => isRunning;

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            setDirection(AggregateFrequency.Value < 0);

            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Volume, AggregateVolume.Value);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Pan, AggregateBalance.Value);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Frequency, bassFreq);
            Bass.ChannelSetAttribute(tempoAdjustStream, ChannelAttribute.Tempo, (Math.Abs(AggregateTempo.Value) - 1) * 100);
        }

        private volatile float initialFrequency;

        private int bassFreq => (int)Math.Clamp(Math.Abs(initialFrequency * AggregateFrequency.Value), 100, 100000);

        private volatile int bitrate;

        public override int? Bitrate => bitrate;

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(activeStream)).CurrentAmplitudes;
    }
}
