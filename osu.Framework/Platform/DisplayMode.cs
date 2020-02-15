// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a display mode on a given <see cref="Display"/>.
    /// </summary>
    public class DisplayMode
    {
        /// <summary>
        /// The pixel format of the display mode, if available.
        /// </summary>
        public string Format;

        /// <summary>
        /// The dimensions of the screen resolution in pixels.
        /// </summary>
        public Size Size;

        /// <summary>
        /// The number of bits that represent the colour value for each pixel.
        /// </summary>
        public int BitsPerPixel;

        /// <summary>
        /// The refresh rate in hertz.
        /// </summary>
        public int RefreshRate;

        public override string ToString() => $"Format: {Format ?? "Unknown"}, Size: {Size}, BitsPerPixel: {BitsPerPixel}, RefreshRate: {RefreshRate}";
    }
}
