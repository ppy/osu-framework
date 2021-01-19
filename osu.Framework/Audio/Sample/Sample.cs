// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    public abstract class Sample : AudioCollectionManager<SampleChannel>
    {
        public const int DEFAULT_CONCURRENCY = 2;

        /// <summary>
        /// The length in milliseconds of this <see cref="Sample"/>.
        /// </summary>
        public double Length { get; protected set; }

        public virtual int PlaybackConcurrency { get; set; } = DEFAULT_CONCURRENCY;

        public SampleChannel Play()
        {
            var channel = CreateChannel();

            if (channel != null)
                AddItem(channel);

            return channel;
        }

        protected abstract SampleChannel CreateChannel();
    }
}
