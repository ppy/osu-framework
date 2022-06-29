// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// Represents all data necessary to describe a single frame of an <see cref="Animation{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of animation the frame data is for.</typeparam>
    public struct FrameData<T>
    {
        /// <summary>
        /// The contents to display for the frame.
        /// </summary>
        public T Content { get; set; }

        /// <summary>
        /// The duration to display the frame for.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// The time at which this frame is displayed in the containing animation.
        /// </summary>
        internal double DisplayStartTime { get; set; }

        /// <summary>
        /// The time at which this frame is no longer displayed.
        /// </summary>
        internal double DisplayEndTime => DisplayStartTime + Duration;
    }
}
