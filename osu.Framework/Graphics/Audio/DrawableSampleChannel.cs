// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;

namespace osu.Framework.Graphics.Audio
{
    public class DrawableSampleChannel : DrawableAudioWrapper
    {
        private readonly SampleChannel channel;

        public DrawableSampleChannel(SampleChannel channel)
            : base(channel)
        {
            this.channel = channel;
        }

        public void Play() => channel.Play();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            channel.Dispose();
        }
    }
}
