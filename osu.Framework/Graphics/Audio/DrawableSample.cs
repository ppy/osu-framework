// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A <see cref="SampleChannel"/> wrapper to allow insertion in the draw hierarchy to allow transforms, lifetime management etc.
    /// </summary>
    public class DrawableSample : DrawableAudioWrapper, ISampleChannel
    {
        private readonly SampleChannel channel;

        /// <summary>
        /// Construct a new drawable sample instance.
        /// </summary>
        /// <param name="channel">The audio sample to wrap.</param>
        /// <param name="disposeChannelOnDisposal">Whether the sample should be automatically disposed on drawable disposal/expiry.</param>
        public DrawableSample(SampleChannel channel, bool disposeChannelOnDisposal = true)
            : base(channel, disposeChannelOnDisposal)
        {
            this.channel = channel;
        }

        public void Play(bool restart = true) => channel.Play(restart);

        public void Stop() => channel.Stop();

        public bool Playing => channel.Playing;

        public bool Played => channel.Played;

        public bool Looping
        {
            get => channel.Looping;
            set => channel.Looping = value;
        }
    }
}
