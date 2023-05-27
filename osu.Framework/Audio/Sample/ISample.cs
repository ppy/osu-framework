// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// An interface for an audio sample.
    /// </summary>
    public interface ISample : IAdjustableAudioComponent
    {
        /// <summary>
        /// A name identifying this sample internally.
        /// </summary>
        string Name { get; }

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
        Bindable<int> PlaybackConcurrency { get; }

        /// <summary>
        /// Creates a new unique playback channel for this <see cref="ISample"/> and immediately plays it.
        /// </summary>
        /// <remarks>
        /// Multiple channels can be played simultaneously, but can only be heard up to <see cref="PlaybackConcurrency"/> times.
        /// </remarks>
        /// <returns>The unique <see cref="SampleChannel"/> for the playback.</returns>
        SampleChannel Play();

        /// <summary>
        /// Retrieves a unique playback channel for this <see cref="ISample"/>.
        /// </summary>
        /// <remarks>
        /// Multiple channels can be retrieved and played simultaneously, but can only be heard up to <see cref="PlaybackConcurrency"/> times.
        /// </remarks>
        /// <returns>The unique <see cref="SampleChannel"/> for the playback.</returns>
        SampleChannel GetChannel();
    }
}
