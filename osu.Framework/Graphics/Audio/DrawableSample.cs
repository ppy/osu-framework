// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A <see cref="SampleChannel"/> wrapper to allow insertion in the draw hierarchy to allow transforms, lifetime management etc.
    /// </summary>
    public class DrawableSample : DrawableAudioWrapper, ISample
    {
        private readonly Sample sample;
        private readonly bool disposeChannelsOnDisposal;

        /// <summary>
        /// Construct a new drawable sample instance.
        /// </summary>
        /// <param name="sample">The audio sample to wrap.</param>
        /// <param name="disposeChannelsOnDisposal">Whether the sample channels should be automatically disposed on drawable disposal/expiry.</param>
        public DrawableSample(Sample sample, bool disposeChannelsOnDisposal = true)
            : base(Empty())
        {
            this.sample = sample;
            this.disposeChannelsOnDisposal = disposeChannelsOnDisposal;
        }

        public SampleChannel Play()
        {
            var channel = sample.Play();
            AddInternal(new DrawableSampleChannel(channel, disposeChannelsOnDisposal));
            return channel;
        }

        public double Length => sample.Length;

        private class DrawableSampleChannel : DrawableAudioWrapper, ISampleChannel
        {
            [NotNull]
            private readonly SampleChannel channel;

            /// <param name="channel">The sample channel to wrap.</param>
            /// <param name="disposeChannelOnDisposal">Whether the channel should be automatically disposed on drawable disposal/expiry.</param>
            public DrawableSampleChannel([NotNull] SampleChannel channel, bool disposeChannelOnDisposal = true)
                : base(channel, disposeChannelOnDisposal)
            {
                this.channel = channel;
            }

            public ChannelAmplitudes CurrentAmplitudes => channel.CurrentAmplitudes;

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
}
