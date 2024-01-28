// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Audio.Mixing.SDL2;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleChannelSDL2 : SampleChannel, ISDL2AudioChannel
    {
        private readonly SampleSDL2AudioPlayer player;

        private volatile bool playing;
        public override bool Playing => playing;

        private volatile bool looping;
        public override bool Looping { get => looping; set => looping = value; }

        public SampleChannelSDL2(SampleSDL2 sample, SampleSDL2AudioPlayer player)
            : base(sample.Name)
        {
            this.player = player;
        }

        public override void Play()
        {
            started = false;
            playing = true;
            base.Play();
        }

        public override void Stop()
        {
            playing = false;
            started = false;
            base.Stop();
        }

        private volatile bool started;

        int ISDL2AudioChannel.GetRemainingSamples(float[] data)
        {
            if (player.RelativeRate != rate)
                player.RelativeRate = rate;

            if (player.Loop != looping)
                player.Loop = looping;

            if (!started)
            {
                player.Reset();
                started = true;
            }

            int ret = player.GetRemainingSamples(data);

            if (player.Done)
            {
                playing = false;
                started = false;
            }

            return ret;
        }

        private volatile float volume = 1.0f;
        private volatile float balance;

        private double rate = 1.0f;

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            volume = (float)AggregateVolume.Value;
            balance = (float)AggregateBalance.Value;

            Interlocked.Exchange(ref rate, AggregateFrequency.Value);
        }

        float ISDL2AudioChannel.Volume => volume;

        bool ISDL2AudioChannel.Playing => playing;

        float ISDL2AudioChannel.Balance => balance;

        ~SampleChannelSDL2()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            (Mixer as SDL2AudioMixer)?.StreamFree(this);

            base.Dispose(disposing);
        }
    }
}
