// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Mixing.SDL2;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleChannelSDL2 : SampleChannel, ISDL2AudioChannel
    {
        private readonly SampleSDL2AudioPlayer player;

        private volatile bool playing;
        public override bool Playing => playing;

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
            if (player.RelativeRate != AggregateFrequency.Value)
                player.RelativeRate = AggregateFrequency.Value;

            if (player.Loop != Looping)
                player.Loop = Looping;

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

        float ISDL2AudioChannel.Volume => (float)AggregateVolume.Value;

        bool ISDL2AudioChannel.Playing => playing;

        double ISDL2AudioChannel.Balance => AggregateBalance.Value;

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
