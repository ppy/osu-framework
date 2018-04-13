// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// Represents a generic, frame-based animation.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// The number of frames this animation has.
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// True if the animation is playing, false otherwise.
        /// </summary>
        bool IsPlaying { get; set; }

        /// <summary>
        /// True if the animation should start over from the first frame after finishing. False if it should stop playing and keep displaying the last frame when finishing.
        /// </summary>
        bool Repeat { get; set; }

        /// <summary>
        /// Displays the frame with the given zero-based frame index.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        void GotoFrame(int frameIndex);
    }
}
