// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Represents a frame decoded from a video.
    /// </summary>
    public class DecodedFrame
    {
        /// <summary>
        /// The timestamp of the frame in the video it was decoded from.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The texture that represents the decoded frame.
        /// </summary>
        public Texture Texture { get; set; }
    }
}
