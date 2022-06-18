// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation / playback sequence.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// The duration of the animation.
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// True if the animation has finished playing, false otherwise.
        /// </summary>
        public bool FinishedPlaying => !Loop && PlaybackPosition > Duration;

        /// <summary>
        /// True if the animation is playing, false otherwise. <c>true</c> by default.
        /// </summary>
        bool IsPlaying { get; set; }

        /// <summary>
        /// True if the animation should start over from the first frame after finishing. False if it should stop playing and keep displaying the last frame when finishing.
        /// </summary>
        bool Loop { get; set; }

        /// <summary>
        /// Seek the animation to a specific time value.
        /// </summary>
        /// <param name="time">The time value to seek to.</param>
        void Seek(double time);

        /// <summary>
        /// The current position of playback.
        /// </summary>
        public double PlaybackPosition { get; set; }
    }
}
