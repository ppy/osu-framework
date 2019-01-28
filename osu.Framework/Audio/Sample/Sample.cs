// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    public abstract class Sample : AudioComponent
    {
        public const int DEFAULT_CONCURRENCY = 2;

        protected readonly int PlaybackConcurrency;

        /// <summary>
        /// Construct a new sample.
        /// </summary>
        /// <param name="playbackConcurrency">How many instances of this sample should be allowed to playback concurrently before stopping the longest playing.</param>
        protected Sample(int playbackConcurrency = DEFAULT_CONCURRENCY)
        {
            PlaybackConcurrency = playbackConcurrency;
        }
    }
}
