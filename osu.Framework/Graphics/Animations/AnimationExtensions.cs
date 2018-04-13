// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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
        /// <param name="animation">The animation that should seek the frame and stop playing.</param>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public static void GotoAndStop(this IAnimation animation, int frameIndex)
        {
            animation.GotoFrame(frameIndex);
            animation.IsPlaying = false;
        }

        /// <summary>
        /// Displays the frame with the given zero-based frame index and plays the animation from that frame.
        /// </summary>
        /// <param name="animation">The animation that should seek the frame and start playing.</param>
        /// <param name="frameIndex">The zero-based index of the frame to display.</param>
        public static void GotoAndPlay(this IAnimation animation, int frameIndex)
        {
            animation.GotoFrame(frameIndex);
            animation.IsPlaying = true;
        }

        /// <summary>
        /// Resumes playing the animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        public static void Play(this IAnimation animation) => animation.IsPlaying = true;

        /// <summary>
        /// Stops playing the animation.
        /// </summary>
        /// <param name="animation">The animation to stop playing.</param>
        public static void Stop(this IAnimation animation) => animation.IsPlaying = false;

        /// <summary>
        /// Restarts the animation.
        /// </summary>
        /// <param name="animation">The animation to restart.</param>
        public static void Restart(this IAnimation animation) => animation.GotoAndPlay(0);
    }
}
