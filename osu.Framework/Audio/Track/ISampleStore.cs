// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;

namespace osu.Framework.Audio.Track
{
    public interface ISampleStore : IAdjustableResourceStore<SampleChannel>
    {
        /// <summary>
        /// How many instances of a single sample should be allowed to playback concurrently before stopping the longest playing.
        /// </summary>
        int PlaybackConcurrency { get; set; }
    }
}
