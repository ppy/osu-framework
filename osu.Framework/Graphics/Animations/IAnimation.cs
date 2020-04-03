// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation / playback sequence.
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// The duration of the video that is being played. Can only be queried after the decoder has started decoding has loaded. This value may be an estimate by FFmpeg, depending on the video loaded.
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// True if the video has finished playing, false otherwise.
        /// </summary>
        public bool FinishedPlaying => !Loop && PlaybackPosition > Duration;

        /// <summary>
        /// True if the animation is playing, false otherwise. Starts true.
        /// </summary>
        bool IsPlaying { get; set; }

        /// <summary>
        /// True if the animation should start over from the first frame after finishing. False if it should stop playing and keep displaying the last frame when finishing.
        /// </summary>
        bool Loop { get; set; }

        /// <summary>
        /// Seek the animation to a specific time value.
        /// </summary>
        /// <param name="time"></param>
        void Seek(double time);

        /// <summary>
        /// The current position of playback.
        /// </summary>
        public double PlaybackPosition { get; set; }
    }
}
