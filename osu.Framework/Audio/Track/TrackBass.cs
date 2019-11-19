// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using osuTK;
using osu.Framework.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using osu.Framework.Audio.Callbacks;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackBass : Track, IBassAudio, IHasPitchAdjust
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
        /// <param name="quick">If true, the track will not be fully loaded, and should only be used for preview purposes.  Defaults to false.</param>
        public TrackBass(Stream data, bool quick = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // todo: support this internally to match the underlying Track implementation (which can support this).
            const float tempo_minimum_supported = 0.05f;

            Tempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackBass)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };

            EnqueueAction(() =>
            {
                Preview = quick;

                //encapsulate incoming stream with async buffer if it isn't already.
                dataStream = data as AsyncBufferStream ?? new AsyncBufferStream(data, quick ? 8 : -1);

                fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(dataStream));

                BassFlags flags = Preview ? 0 : BassFlags.Decode | BassFlags.Prescan;
                activeStream = Bass.CreateStream(StreamSystem.NoBuffer, flags, fileCallbacks.Callbacks, fileCallbacks.Handle);

                if (!Preview)
                {
                    // We assign the BassFlags.Decode streams to the device "bass_nodevice" to prevent them from getting
                    // cleaned up during a Bass.Free call. This is necessary for seamless switching between audio devices.
                    // Further, we provide the flag BassFlags.FxFreeSource such that freeing the activeStream also frees
                    // all parent decoding streams.
                    const int bass_nodevice = 0x20000;

                    Bass.ChannelSetDevice(activeStream, bass_nodevice);
                    tempoAdjustStream = BassFx.TempoCreate(activeStream, BassFlags.Decode | BassFlags.FxFreeSource);
                    Bass.ChannelSetDevice(tempoAdjustStream, bass_nodevice);
                    activeStream = BassFx.ReverseCreate(tempoAdjustStream, 5f, BassFlags.Default | BassFlags.FxFreeSource);

                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoOverlapMilliseconds, 4);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoSequenceMilliseconds, 30);
                }

                // will be -1 in case of an error
                double seconds = Bass.ChannelBytes2Seconds(activeStream, byteLength = Bass.ChannelGetLength(activeStream));

                bool success = seconds >= 0;

                if (success)
                {
                    Length = seconds * 1000;

                    // Bass does not allow seeking to the end of the track, so the last available position is 1 sample before.
                    lastSeekablePosition = Bass.ChannelBytes2Seconds(activeStream, byteLength - BYTES_PER_SAMPLE) * 1000;

                    Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Frequency, out float frequency);
                    initialFrequency = frequency;
                    bitrate = (int)Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Bitrate);

                    stopCallback = new SyncCallback((a, b, c, d) => RaiseFailed());
                    endCallback = new SyncCallback((a, b, c, d) =>
                    {
                        if (!Looping)
                            RaiseCompleted();
                    });

                    Bass.ChannelSetSync(activeStream, SyncFlags.Stop, 0, stopCallback.Callback, stopCallback.Handle);
                    Bass.ChannelSetSync(activeStream, SyncFlags.End, 0, endCallback.Callback, endCallback.Handle);

                    isLoaded = true;
                }
            });

            InvalidateState();
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            Bass.ChannelSetDevice(activeStream, deviceIndex);
            Trace.Assert(Bass.LastError == Errors.OK);
        }

        protected override void UpdateState()
        {
            isRunning = Bass.ChannelIsActive(activeStream) == PlaybackState.Playing;

            Interlocked.Exchange(ref currentTime, Bass.ChannelBytes2Seconds(activeStream, Bass.ChannelGetPosition(activeStream)) * 1000);

            var leftChannel = isPlayed ? Bass.ChannelGetLevelLeft(activeStream) / 32768f : -1;
            var rightChannel = isPlayed ? Bass.ChannelGetLevelRight(activeStream) / 32768f : -1;

            if (leftChannel >= 0 && rightChannel >= 0)
            {
                currentAmplitudes.LeftChannel = leftChannel;
                currentAmplitudes.RightChannel = rightChannel;

                float[] tempFrequencyData = new float[256];
                Bass.ChannelGetData(activeStream, tempFrequencyData, (int)DataFlags.FFT512);
                currentAmplitudes.FrequencyAmplitudes = tempFrequencyData;
            }
            else
            {
                currentAmplitudes.LeftChannel = 0;
                currentAmplitudes.RightChannel = 0;
                currentAmplitudes.FrequencyAmplitudes = new float[256];
            }

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
            if (Bass.ChannelIsActive(activeStream) == PlaybackState.Playing)
                Bass.ChannelPause(activeStream);

            isPlayed = false;
        });

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
            // Bass will restart the track if it has reached its end. This behavior isn't desirable so block locally.
            if (Bass.ChannelGetPosition(activeStream) == byteLength)
                return;

            if (Bass.ChannelPlay(activeStream))
                isPlayed = true;
            else
                isRunning = false;
        });

        public override bool Seek(double seek) => SeekAsync(seek).Result;

        public async Task<bool> SeekAsync(double seek)
        {
            // At this point the track may not yet be loaded which is indicated by a 0 length.
            // In that case we still want to return true, hence the conservative length.
            double conservativeLength = Length == 0 ? double.MaxValue : lastSeekablePosition;
            double conservativeClamped = MathHelper.Clamp(seek, 0, conservativeLength);

            await EnqueueAction(() =>
            {
                double clamped = MathHelper.Clamp(seek, 0, Length);

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
            Bass.ChannelSetAttribute(tempoAdjustStream, ChannelAttribute.Tempo, (Math.Abs(Tempo.Value) - 1) * 100);
        }

        private volatile float initialFrequency;

        private int bassFreq => (int)MathHelper.Clamp(Math.Abs(initialFrequency * AggregateFrequency.Value), 100, 100000);

        private volatile int bitrate;

        public override int? Bitrate => bitrate;

        public double PitchAdjust
        {
            get => Frequency.Value;
            set => Frequency.Value = value;
        }

        private TrackAmplitudes currentAmplitudes;

        public override TrackAmplitudes CurrentAmplitudes => currentAmplitudes;
    }
}
