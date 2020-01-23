// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// An audio track.
    /// </summary>
    public interface ITrack
    {
        /// <summary>
        /// States if this track should repeat.
        /// </summary>
        bool Looping { get; set; }

        /// <summary>
        /// Point in time in milliseconds to restart the track to on loop or <see cref="Restart"/>.
        /// </summary>
        double RestartPoint { get; set; }

        /// <summary>
        /// Current position in milliseconds.
        /// </summary>
        double CurrentTime { get; }

        /// <summary>
        /// Length of the track in milliseconds.
        /// </summary>
        double Length { get; set; }

        bool IsRunning { get; }

        /// <summary>
        /// Reset this track to a logical default state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Restarts this track from the <see cref="Track.RestartPoint"/> while retaining adjustments.
        /// </summary>
        void Restart();

        void ResetSpeedAdjustments();

        /// <summary>
        /// Seek to a new position.
        /// </summary>
        /// <param name="seek">New position in milliseconds</param>
        /// <returns>Whether the seek was successful.</returns>
        bool Seek(double seek);

        /// <summary>
        /// Start playback.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop playback.
        /// </summary>
        void Stop();
    }
}
