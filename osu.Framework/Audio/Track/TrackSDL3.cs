// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Dsp;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Extensions;
using SDL;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackSDL3 : Track, ISDL3AudioChannel, ISDL3AudioDataReceiver
    {
        private readonly TempoSDL3AudioPlayer player;

        public override bool IsDummyDevice => false;

        private volatile bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private volatile bool isCompletelyLoaded;

        /// <summary>
        /// Audio can be played without interrupt once it's set to true. <see cref="IsLoaded"/> means that it at least has 'some' data to play.
        /// </summary>
        public bool IsCompletelyLoaded => isCompletelyLoaded;

        private double currentTime;
        public override double CurrentTime => currentTime;

        private volatile bool isRunning;
        public override bool IsRunning => isRunning;

        private volatile bool hasCompleted;
        public override bool HasCompleted => hasCompleted;

        private volatile int bitrate;
        public override int? Bitrate => bitrate;

        public TrackSDL3(string name, SDL_AudioSpec spec, int samples)
            : base(name)
        {
            // SoundTouch limitation
            const float tempo_minimum_supported = 0.05f;
            AggregateTempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackSDL3)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };

            player = new TempoSDL3AudioPlayer(spec.freq, spec.channels, samples);
        }

        private readonly object syncRoot = new object();

        private SDL3AudioDecoder? decodeData;

        void ISDL3AudioDataReceiver.GetData(byte[] audio, int length, SDL3AudioDecoder data, bool done)
        {
            if (IsDisposed)
                return;

            lock (syncRoot)
            {
                if (!player.IsLoaded)
                {
                    if (!player.IsLoading)
                        player.PrepareStream(data.ByteLength);

                    player.PutSamplesInStream(audio, length);

                    if (done)
                        player.DonePutting();
                }
            }

            if (!isLoaded)
                Interlocked.Exchange(ref decodeData, data);
        }

        private volatile bool amplitudeRequested;
        private double lastTime;

        private ChannelAmplitudes currentAmplitudes = ChannelAmplitudes.Empty;
        private float[]? samples;
        private Complex[]? fftSamples;
        private float[]? fftResult;

        public override ChannelAmplitudes CurrentAmplitudes
        {
            get
            {
                if (!amplitudeRequested)
                    amplitudeRequested = true;

                return isRunning ? currentAmplitudes : ChannelAmplitudes.Empty;
            }
        }

        private void updateCurrentAmplitude()
        {
            samples ??= new float[(int)(player.SrcRate * (1f / 60)) * player.SrcChannels];
            fftSamples ??= new Complex[ChannelAmplitudes.AMPLITUDES_SIZE * 2];
            fftResult ??= new float[ChannelAmplitudes.AMPLITUDES_SIZE];

            player.Peek(samples, lastTime);

            float leftAmplitude = 0;
            float rightAmplitude = 0;
            int secondCh = player.SrcChannels < 2 ? 0 : 1;
            int fftIndex = 0;

            for (int i = 0; i < samples.Length; i += player.SrcChannels)
            {
                leftAmplitude = Math.Max(leftAmplitude, Math.Abs(samples[i]));
                rightAmplitude = Math.Max(rightAmplitude, Math.Abs(samples[i + secondCh]));

                if (fftIndex < fftSamples.Length)
                {
                    fftSamples[fftIndex].Y = 0;
                    fftSamples[fftIndex++].X = samples[i] + samples[i + secondCh];
                }
            }

            FastFourierTransform.FFT(true, (int)Math.Log2(fftSamples.Length), fftSamples);

            for (int i = 0; i < fftResult.Length; i++)
                fftResult[i] = fftSamples[i].ComputeMagnitude();

            currentAmplitudes = new ChannelAmplitudes(Math.Min(1f, leftAmplitude), Math.Min(1f, rightAmplitude), fftResult);
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            if (decodeData != null)
            {
                if (!isLoaded)
                {
                    Length = decodeData.Length;
                    bitrate = decodeData.Bitrate;
                    isLoaded = true;
                }

                if (player.IsLoaded)
                {
                    Length = player.AudioLength;
                    isCompletelyLoaded = true;
                    decodeData = null;
                }
            }

            if (player.Done && isRunning)
            {
                if (Looping)
                {
                    seekInternal(RestartPoint);
                }
                else
                {
                    isRunning = false;
                    hasCompleted = true;
                    RaiseCompleted();
                }
            }

            if (AggregateTempo.Value != 1 && isRunning)
            {
                lock (syncRoot)
                    player.FillRequiredSamples();
            }

            // Not sure if I need to split this up to another class since this feature is only exclusive to Track
            if (amplitudeRequested && isRunning && Math.Abs(currentTime - lastTime) > 1000.0 / 60.0)
            {
                lastTime = currentTime;

                updateCurrentAmplitude();
            }
        }

        public override bool Seek(double seek) => SeekAsync(seek).GetResultSafely();

        public override async Task<bool> SeekAsync(double seek)
        {
            double conservativeLength = Length == 0 ? double.MaxValue : Length;
            double conservativeClamped = Math.Clamp(seek, 0, conservativeLength);

            await EnqueueAction(() => seekInternal(seek)).ConfigureAwait(false);

            return conservativeClamped == seek;
        }

        private void seekInternal(double seek)
        {
            double time;

            lock (syncRoot)
            {
                player.Seek(seek);

                if (seek < Length)
                {
                    player.Reset(false);
                    hasCompleted = false;
                }

                time = player.GetCurrentTime();
            }

            Interlocked.Exchange(ref currentTime, time);
        }

        public override void Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not start disposed tracks.");

            StartAsync().WaitSafely();
        }

        public override Task StartAsync() => EnqueueAction(() =>
        {
            lock (syncRoot)
                player.Reset(false);

            isRunning = true;
            hasCompleted = false;
        });

        public override void Stop() => StopAsync().WaitSafely();

        public override Task StopAsync() => EnqueueAction(() =>
        {
            isRunning = false;
        });

        int ISDL3AudioChannel.GetRemainingSamples(float[] data)
        {
            if (!IsLoaded) return 0;

            int ret;
            double time;

            lock (syncRoot)
            {
                ret = player.GetRemainingSamples(data);
                time = player.GetCurrentTime();
            }

            Interlocked.Exchange(ref currentTime, time);

            if (ret < 0)
            {
                EnqueueAction(RaiseFailed);
                return 0;
            }

            return ret;
        }

        private (float, float) volume = (1.0f, 1.0f);

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            lock (syncRoot)
            {
                if (!player.ReversePlayback && AggregateFrequency.Value < 0)
                    player.ReversePlayback = true;
                else if (player.ReversePlayback && AggregateFrequency.Value >= 0)
                    player.ReversePlayback = false;

                player.RelativeRate = Math.Abs(AggregateFrequency.Value);
                player.Tempo = AggregateTempo.Value;
            }

            volume = ((float, float))Adjustments.GetAggregatedStereoVolume();
        }

        bool ISDL3AudioChannel.Playing => isRunning && !player.Done;

        (float, float) ISDL3AudioChannel.Volume => volume;

        ~TrackSDL3()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            isRunning = false;
            (Mixer as SDL3AudioMixer)?.StreamFree(this);

            decodeData?.Stop();
            decodeData = null;

            lock (syncRoot)
                player.Dispose();

            base.Dispose(disposing);
        }
    }
}
