// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    public interface ISampleStore : IAdjustableResourceStore<SampleChannel>
    {
        /// <summary>
        /// How many instances of a single sample should be allowed to playback concurrently before stopping the longest playing.
        /// </summary>
        int PlaybackConcurrency { get; set; }

        /// <summary>
        /// Retrieves a new channel for the specified sample.
        /// </summary>
        /// <param name="sampleName">The name of the sample.</param>
        /// <returns>A new channel for the specified sample..</returns>
        new SampleChannel Get(string sampleName);

        /// <summary>
        /// Retrieves a new channel for the specified sample which will play a new layered instance when <see cref="SampleChannel.Play"/> is invoked.
        /// </summary>
        /// <param name="sampleName">The name of the sample.</param>
        /// <returns>A new channel for the specified sample.</returns>
        SampleChannel GetLayerable(string sampleName);
    }
}
