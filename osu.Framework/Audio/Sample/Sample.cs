// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample
{
    public abstract class Sample : AudioCollectionManager<SampleChannel>, ISample
    {
        public const int DEFAULT_CONCURRENCY = 2;

        public double Length { get; protected set; }

        public Bindable<int> PlaybackConcurrency { get; } = new Bindable<int>(DEFAULT_CONCURRENCY);

        public SampleChannel Play()
        {
            var channel = GetChannel();
            channel.Play();
            return channel;
        }

        public SampleChannel GetChannel()
        {
            var channel = CreateChannel();

            if (channel != null)
                AddItem(channel);

            return channel;
        }

        /// <summary>
        /// Creates a unique playback of this <see cref="Sample"/>.
        /// </summary>
        /// <returns>The <see cref="SampleChannel"/> for the playback.</returns>
        protected abstract SampleChannel CreateChannel();
    }
}
