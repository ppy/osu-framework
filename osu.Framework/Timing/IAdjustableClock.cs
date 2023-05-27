﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock that can be started, stopped, reset etc.
    /// </summary>
    public interface IAdjustableClock : IClock
    {
        /// <summary>
        /// Stop and reset position.
        /// </summary>
        void Reset();

        /// <summary>
        /// Start (resume) running.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop (pause) running.
        /// </summary>
        void Stop();

        /// <summary>
        /// Seek to a specific time position.
        /// </summary>
        /// <returns>Whether a seek was possible.</returns>
        bool Seek(double position);

        /// <summary>
        /// The rate this clock is running at, relative to real-time.
        /// </summary>
        new double Rate { get; set; }

        /// <summary>
        /// Reset the rate to a stable value.
        /// </summary>
        void ResetSpeedAdjustments();
    }
}
