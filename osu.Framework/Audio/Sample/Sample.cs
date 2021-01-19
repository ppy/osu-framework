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

        protected readonly int PlaybackConcurrency;

        /// <summary>
        /// Construct a new sample.
        /// </summary>
        /// <param name="playbackConcurrency">How many instances of this sample should be allowed to playback concurrently before stopping the longest playing.</param>
        protected Sample(int playbackConcurrency = DEFAULT_CONCURRENCY)
        {
            PlaybackConcurrency = playbackConcurrency;
        }

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
