// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// This class holds various extension methods for the <see cref="IAnimation"/> interface.
    /// </summary>
    public static class AnimationExtensions
    {
        /// <summary>
        /// Displays the frame with the given zero-based frame index and stops the animation at that frame.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public static void GotoAndStop(this IAnimation animation, int frameIndex)
        {
            animation.GotoFrame(frameIndex);
            animation.IsPlaying = false;
        }

        /// <summary>
        /// Displays the frame with the given zero-based frame index and plays the animation from that frame.
        /// </summary>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public static void GotoAndPlay(this IAnimation animation, int frameIndex)
        {
            animation.GotoFrame(frameIndex);
            animation.IsPlaying = true;
        }
    }
}
