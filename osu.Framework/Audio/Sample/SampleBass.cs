// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleBass : Sample
    {
        public int SampleId => factory.SampleId;

        public override bool IsLoaded => factory.IsLoaded;

        private readonly SampleBassFactory factory;

        internal SampleBass(SampleBassFactory factory)
        {
            this.factory = factory;

            PlaybackConcurrency.BindTo(factory.PlaybackConcurrency);
        }

        protected override SampleChannel CreateChannel() => new SampleChannelBass(this);
    }
}
