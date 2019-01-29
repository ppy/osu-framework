// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which will only update its current time when a frame proces is triggered.
    /// Useful for keeping a consistent time state across an individual update.
    /// </summary>
    public interface IFrameBasedClock : IClock
    {
        /// <summary>
        /// Elapsed time since last frame in milliseconds.
        /// </summary>
        double ElapsedFrameTime { get; }

        double FramesPerSecond { get; }

        FrameTimeInfo TimeInfo { get; }

        /// <summary>
        /// Processes one frame. Generally should be run once per update loop.
        /// </summary>
        void ProcessFrame();
    }
}
