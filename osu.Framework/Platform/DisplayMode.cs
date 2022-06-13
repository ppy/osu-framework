// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a display mode on a given <see cref="Display"/>.
    /// </summary>
    public readonly struct DisplayMode : IEquatable<DisplayMode>
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
        /// The index of the display this mode belongs to as determined by the windowing backend.
        /// </summary>
        public readonly int DisplayIndex;

        public DisplayMode(string format, Size size, int bitsPerPixel, int refreshRate, int displayIndex)
        {
            Format = format ?? "Unknown";
            Size = size;
            BitsPerPixel = bitsPerPixel;
            RefreshRate = refreshRate;
            DisplayIndex = displayIndex;
        }

        public override string ToString() => $"Size: {Size}, BitsPerPixel: {BitsPerPixel}, RefreshRate: {RefreshRate}, Format: {Format}, DisplayIndex: {DisplayIndex}";

        public bool Equals(DisplayMode other) =>
            Format == other.Format
            && Size == other.Size
            && BitsPerPixel == other.BitsPerPixel
            && RefreshRate == other.RefreshRate
            && DisplayIndex == other.DisplayIndex;

        public static bool operator ==(DisplayMode left, DisplayMode right) => left.Equals(right);
        public static bool operator !=(DisplayMode left, DisplayMode right) => !(left == right);

        public override bool Equals(object obj) => obj is DisplayMode other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Format, Size, BitsPerPixel, RefreshRate, DisplayIndex);
    }
}
