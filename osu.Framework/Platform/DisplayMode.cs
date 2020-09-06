// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a display mode on a given <see cref="Display"/>.
    /// </summary>
    public readonly struct DisplayMode
    {
        /// <summary>
        /// The pixel format of the display mode, if available.
        /// </summary>
        public readonly string Format;

        /// <summary>
        /// The dimensions of the screen resolution in pixels.
        /// </summary>
        public readonly Size Size;

        /// <summary>
        /// The number of bits that represent the colour value for each pixel.
        /// </summary>
        public readonly int BitsPerPixel;

        /// <summary>
        /// The refresh rate in hertz.
        /// </summary>
        public readonly int RefreshRate;

        /// <summary>
        /// The index of the display mode as determined by the windowing backend.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The index of the display this mode belongs to as determined by the windowing backend.
        /// </summary>
        public readonly int DisplayIndex;

        public DisplayMode(string format, Size size, int bitsPerPixel, int refreshRate, int index, int displayIndex)
        {
            Format = format ?? "Unknown";
            Size = size;
            BitsPerPixel = bitsPerPixel;
            RefreshRate = refreshRate;
            Index = index;
            DisplayIndex = displayIndex;
        }

        public override string ToString() => $"Size: {Size}, BitsPerPixel: {BitsPerPixel}, RefreshRate: {RefreshRate}, Format: {Format}, Index: {Index}, DisplayIndex: {DisplayIndex}";
    }
}
