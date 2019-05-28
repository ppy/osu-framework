// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;

namespace osu.Framework.Graphics.Audio
{
    public class DrawableSampleChannel : DrawableAudioWrapper, ISampleChannel
    {
        private readonly SampleChannel channel;

        public DrawableSampleChannel(SampleChannel channel, bool disposeSampleOnDisposal = true)
            : base(channel, disposeSampleOnDisposal)
        {
            this.channel = channel;
        }

        public void Play(bool restart = true) => channel.Play(restart);

        public void Stop() => channel.Stop();

        public bool Playing => channel.Playing;

        public bool Played => channel.Played;
    }
}
