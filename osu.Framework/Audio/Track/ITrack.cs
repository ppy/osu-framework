// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// An audio track.
    /// </summary>
    public interface ITrack : IHasAmplitudes, IAdjustableAudioComponent
    {
        /// <summary>
        /// Invoked when this track has completed.
        /// </summary>
        event Action Completed;

        /// <summary>
        /// Invoked when this track has failed.
        /// </summary>
        event Action Failed;

        /// <summary>
        /// States if this track should repeat.
        /// </summary>
        bool Looping { get; set; }

        /// <summary>
        /// Is this track capable of producing audio?
        /// </summary>
        bool IsDummyDevice { get; }

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

        /// <summary>
        /// The bitrate of this track.
        /// </summary>
        int? Bitrate { get; }

        /// <summary>
        /// Whether this track is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Whether this track is reversed.
        /// </summary>
        bool IsReversed { get; }

        /// <summary>
        /// Whether this track has finished playing back.
        /// </summary>
        public bool HasCompleted { get; }

        /// <summary>
        /// Reset this track to a logical default state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Restarts this track from the <see cref="Track.RestartPoint"/> while retaining adjustments.
        /// </summary>
        void Restart();

        /// <summary>
        /// Removes all speed adjustments added to this track.
        /// </summary>
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
