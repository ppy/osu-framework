// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using OpenTK;
using osu.Framework.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace osu.Framework.Audio.Track
{
    public class TrackBass : Track, IBassAudio, IHasPitchAdjust
    {
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

        private volatile bool isLoaded;

        public override bool IsLoaded => isLoaded;

        public TrackBass(Stream data, bool quick = false)
        {
            EnqueueAction(() =>
            {
                Preview = quick;

                if (data == null)
                    throw new ArgumentNullException(nameof(data));
                //encapsulate incoming stream with async buffer if it isn't already.
                dataStream = data as AsyncBufferStream ?? new AsyncBufferStream(data, quick ? 8 : -1);

                var procs = new DataStreamFileProcedures(dataStream);

                BassFlags flags = Preview ? 0 : BassFlags.Decode | BassFlags.Prescan | BassFlags.Float;
                activeStream = Bass.CreateStream(StreamSystem.NoBuffer, flags, procs.BassProcedures, IntPtr.Zero);

                if (!Preview)
                {
                    // We assign the BassFlags.Decode streams to the device "bass_nodevice" to prevent them from getting
                    // cleaned up during a Bass.Free call. This is necessary for seamless switching between audio devices.
                    // Further, we provide the flag BassFlags.FxFreeSource such that freeing the activeStream also frees
                    // all parent decoding streams.
                    const int bass_nodevice = 0x20000;

                    Bass.ChannelSetDevice(activeStream, bass_nodevice);
                    tempoAdjustStream = BassFx.TempoCreate(activeStream, BassFlags.Decode | BassFlags.FxFreeSource);
                    Bass.ChannelSetDevice(activeStream, bass_nodevice);
                    activeStream = BassFx.ReverseCreate(tempoAdjustStream, 5f, BassFlags.Default | BassFlags.FxFreeSource);

                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoUseQuickAlgorithm, 1);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoOverlapMilliseconds, 4);
                    Bass.ChannelSetAttribute(activeStream, ChannelAttribute.TempoSequenceMilliseconds, 30);
                }

                Length = Bass.ChannelBytes2Seconds(activeStream, Bass.ChannelGetLength(activeStream)) * 1000;

                Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Frequency, out float frequency);
                initialFrequency = frequency;
                bitrate = (int)Bass.ChannelGetAttribute(activeStream, ChannelAttribute.Bitrate);

                isLoaded = true;
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

            double currentTimeLocal = Bass.ChannelBytes2Seconds(activeStream, Bass.ChannelGetPosition(activeStream)) * 1000;
            Interlocked.Exchange(ref currentTime, currentTimeLocal == Length && !isPlayed ? 0 : currentTimeLocal);

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

            base.Dispose(disposing);
        }

        public override bool IsDummyDevice => false;

        public override void Stop()
        {
            base.Stop();
            StopAsync().Wait();
        }

        public async Task StopAsync()
        {
            await EnqueueAction(() =>
            {
                if (Bass.ChannelIsActive(activeStream) == PlaybackState.Playing)
                    Bass.ChannelPause(activeStream);

                isPlayed = false;
            });
        }

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

        public async Task StartAsync()
        {
            await EnqueueAction(() =>
            {
                if (Bass.ChannelPlay(activeStream))
                    isPlayed = true;
                else
                    isRunning = false;
            });
        }

        public override bool Seek(double seek) => SeekAsync(seek).Result;

        public async Task<bool> SeekAsync(double seek)
        {
            // At this point the track may not yet be loaded which is indicated by a 0 length.
            // In that case we still want to return true, hence the conservative length.
            double conservativeLength = Length == 0 ? double.MaxValue : Length;
            double conservativeClamped = MathHelper.Clamp(seek, 0, conservativeLength);

            await EnqueueAction(() =>
            {
                double clamped = MathHelper.Clamp(seek, 0, Length);

                if (clamped != CurrentTime)
                {
                    long pos = Bass.ChannelSeconds2Bytes(activeStream, clamped / 1000d);
                    Bass.ChannelSetPosition(activeStream, pos);
                }
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

            setDirection(FrequencyCalculated.Value < 0);

            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Volume, VolumeCalculated);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Pan, BalanceCalculated);
            Bass.ChannelSetAttribute(activeStream, ChannelAttribute.Frequency, bassFreq);
            Bass.ChannelSetAttribute(tempoAdjustStream, ChannelAttribute.Tempo, (Math.Abs(Tempo) - 1) * 100);
        }

        private volatile float initialFrequency;

        private int bassFreq => (int)MathHelper.Clamp(Math.Abs(initialFrequency * FrequencyCalculated), 100, 100000);

        private volatile int bitrate;

        public override int? Bitrate => bitrate;

        public double PitchAdjust
        {
            get { return Frequency.Value; }
            set { Frequency.Value = value; }
        }

        private TrackAmplitudes currentAmplitudes;

        public override TrackAmplitudes CurrentAmplitudes => currentAmplitudes;
    }
}
