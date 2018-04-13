// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        /// Constructs new frame data with the given content and duration.
        /// </summary>
        /// <param name="content">The content of the frame.</param>
        /// <param name="duration">The duration the frame will be displayed for.</param>
        public FrameData(T content, double duration)
        {
            Content = content;
            Duration = duration;
        }
    }
}
