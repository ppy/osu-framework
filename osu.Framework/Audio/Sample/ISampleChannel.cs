// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A unique playback of an <see cref="ISample"/>.
    /// </summary>
    public interface ISampleChannel : IHasAmplitudes
    {
        /// <summary>
        /// A name identifying this sample internally.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Starts or resumes playback. Has no effect if this <see cref="ISampleChannel"/> is already playing.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops playback.
        /// </summary>
        void Stop();

        /// <summary>
        /// Whether playback was ever started.
        /// </summary>
        bool Played { get; }

        /// <summary>
        /// Whether playback is currently in progress.
        /// </summary>
        bool Playing { get; }

        /// <summary>
        /// Whether playback should repeat.
        /// </summary>
        bool Looping { get; set; }
    }
}
