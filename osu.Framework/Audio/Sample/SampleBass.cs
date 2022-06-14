// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Mixing.Bass;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleBass : Sample
    {
        public int SampleId => factory.SampleId;

        public override bool IsLoaded => factory.IsLoaded;

        private readonly SampleBassFactory factory;
        private readonly BassAudioMixer mixer;

        internal SampleBass(SampleBassFactory factory, BassAudioMixer mixer)
        {
            this.factory = factory;
            this.mixer = mixer;

            PlaybackConcurrency.BindTo(factory.PlaybackConcurrency);
        }

        protected override SampleChannel CreateChannel()
        {
            var channel = new SampleChannelBass(this);
            mixer.Add(channel);
            return channel;
        }
    }
}
