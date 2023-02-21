// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    public interface ISampleStore : IAdjustableResourceStore<Sample>
    {
        /// <summary>
        /// How many instances of a single sample should be allowed to playback concurrently before stopping the longest playing.
        /// </summary>
        int PlaybackConcurrency { get; set; }

        /// <summary>
        /// Add a file extension to automatically append to any lookups on this store.
        /// </summary>
        void AddExtension(string extension);
    }
}
