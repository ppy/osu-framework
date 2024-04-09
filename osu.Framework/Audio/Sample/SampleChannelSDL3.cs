// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Audio.Mixing.SDL3;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleChannelSDL3 : SampleChannel, ISDL3AudioChannel
    {
        private readonly SampleSDL3AudioPlayer player;

        private volatile bool playing;
        public override bool Playing => playing;

        private volatile bool looping;
        public override bool Looping { get => looping; set => looping = value; }

        public SampleChannelSDL3(SampleSDL3 sample, SampleSDL3AudioPlayer player)
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

        int ISDL3AudioChannel.GetRemainingSamples(float[] data)
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

        private (float, float) volume = (1.0f, 1.0f);

        private double rate = 1.0f;

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            double balance = AggregateBalance.Value;
            volume = ((float)(AggregateVolume.Value * (balance > 0 ? balance : 1.0)), (float)(AggregateVolume.Value * (balance < 0 ? -balance : 1.0)));

            Interlocked.Exchange(ref rate, AggregateFrequency.Value);
        }

        (float, float) ISDL3AudioChannel.Volume => volume;

        bool ISDL3AudioChannel.Playing => playing;

        ~SampleChannelSDL3()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            (Mixer as SDL3AudioMixer)?.StreamFree(this);

            base.Dispose(disposing);
        }
    }
}
