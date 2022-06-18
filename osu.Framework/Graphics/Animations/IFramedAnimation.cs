// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation with well-defined frames.
    /// </summary>
    public interface IFramedAnimation : IAnimation
    {
        /// <summary>
        /// The number of frames this animation has.
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// The currently visible frame's index.
        /// </summary>
        int CurrentFrameIndex { get; }

        /// <summary>
        /// Displays the frame with the given zero-based frame index.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        void GotoFrame(int frameIndex);
    }
}
