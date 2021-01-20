// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// An interface for an audio sample.
    /// </summary>
    public interface ISample
    {
        /// <summary>
        /// The length in milliseconds of this <see cref="ISample"/>.
        /// </summary>
        double Length { get; }

        /// <summary>
        /// The number of times this sample (as identified by name) can be played back concurrently.
        /// </summary>
        /// <remarks>
        /// This affects all <see cref="ISample"/> instances identified by the same sample name.
        /// </remarks>
        int PlaybackConcurrency { get; set; }

        /// <summary>
        /// Plays this <see cref="ISample"/> in a unique playback channel.
        /// </summary>
        /// <remarks>
        /// Concurrent playbacks are supported by calling this method multiple times, but can only be heard up to <see cref="PlaybackConcurrency"/> times.
        /// If concurrent playback is not desired, stop any <see cref="SampleChannel"/> returned from a previous call to <see cref="Play"/>.
        /// </remarks>
        /// <returns>The <see cref="SampleChannel"/> for the playback.</returns>
        SampleChannel Play();
    }
}
