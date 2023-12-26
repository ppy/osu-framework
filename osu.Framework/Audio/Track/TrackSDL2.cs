// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Dsp;
using osu.Framework.Audio.Mixing.SDL2;
using osu.Framework.Extensions;
using osu.Framework.Utils;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackSDL2 : Track, ISDL2AudioChannel
    {
        private readonly TempoSDL2AudioPlayer player;

        public override bool IsDummyDevice => false;

        private volatile bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private double currentTime;
        public override double CurrentTime => currentTime;

        private volatile bool isRunning;
        public override bool IsRunning => isRunning;

        private volatile bool hasCompleted;
        public override bool HasCompleted => hasCompleted;

        private volatile int bitrate;
        public override int? Bitrate => bitrate;

        public TrackSDL2(string name, int rate, byte channels, int samples)
            : base(name)
        {
            // SoundTouch limitation
            const float tempo_minimum_supported = 0.05f;
            AggregateTempo.ValueChanged += t =>
            {
                if (t.NewValue < tempo_minimum_supported)
                    throw new ArgumentException($"{nameof(TrackSDL2)} does not support {nameof(Tempo)} specifications below {tempo_minimum_supported}. Use {nameof(Frequency)} instead.");
            };

            player = new TempoSDL2AudioPlayer(rate, channels, samples);
        }

        private readonly object syncRoot = new object();

        private AudioDecoderManager.AudioDecoder? decodeData;

        internal void ReceiveAudioData(byte[] audio, int length, AudioDecoderManager.AudioDecoder data, bool done)
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

            lock (syncRoot)
                player.Peek(samples);

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
                    fftSamples[fftIndex++].X = (samples[i] + samples[i + secondCh]) * 0.5f;
                }
            }

            FastFourierTransform.FFT(true, (int)Math.Log2(fftSamples.Length), fftSamples);

            for (int i = 0; i < fftResult.Length; i++)
                fftResult[i] = (float)Math.Sqrt(fftSamples[i].X * fftSamples[i].X + fftSamples[i].Y + fftSamples[i].Y);

            currentAmplitudes = new ChannelAmplitudes(Math.Min(1f, leftAmplitude), Math.Min(1f, rightAmplitude), fftResult);
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            if (decodeData != null && !isLoaded)
            {
                if (isLoaded)
                {
                    decodeData = null;
                }
                else
                {
                    Length = decodeData.Length;
                    bitrate = decodeData.Bitrate;
                    isLoaded = true;
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

            // Not sure if I need to split this up to another class since this featrue is only exclusive to Track
            if (amplitudeRequested && isRunning && currentTime != lastTime)
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
            lock (syncRoot)
            {
                player.Seek(seek);

                if (seek < Length)
                {
                    player.Reset(false);
                    hasCompleted = false;
                }

                Interlocked.Exchange(ref currentTime, player.GetCurrentTime());
            }
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

        int ISDL2AudioChannel.GetRemainingSamples(float[] data)
        {
            if (!IsLoaded) return 0;

            int ret;

            lock (syncRoot)
            {
                ret = player.GetRemainingSamples(data);
                Interlocked.Exchange(ref currentTime, player.GetCurrentTime());
            }

            if (ret < 0)
            {
                EnqueueAction(RaiseFailed);
                return 0;
            }

            return ret;
        }

        private volatile float volume = 1.0f;
        private volatile float balance;

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

            volume = (float)AggregateVolume.Value;
            balance = (float)AggregateBalance.Value;
        }

        bool ISDL2AudioChannel.Playing => isRunning && !player.Done;

        float ISDL2AudioChannel.Volume => volume;

        float ISDL2AudioChannel.Balance => balance;

        ~TrackSDL2()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            isRunning = false;
            (Mixer as SDL2AudioMixer)?.StreamFree(this);

            decodeData?.Stop();

            lock (syncRoot)
                player.Dispose();

            base.Dispose(disposing);
        }
    }
}
