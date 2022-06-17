// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;
using System.Linq;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a physical display device on the current system.
    /// </summary>
    public sealed class Display : IEquatable<Display>
    {
        /// <summary>
        /// The name of the display, if available. Usually the manufacturer.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The current rectangle of the display in screen space.
        /// Non-zero X and Y values represent a non-primary monitor, and indicate its position
        /// relative to the primary monitor.
        /// </summary>
        public Rectangle Bounds { get; }

        /// <summary>
        /// The available <see cref="DisplayMode"/>s on this display.
        /// </summary>
        public DisplayMode[] DisplayModes { get; }

        /// <summary>
        /// The zero-based index of the <see cref="Display"/>.
        /// </summary>
        public int Index { get; }

        public Display(int index, string name, Rectangle bounds, DisplayMode[] displayModes)
        {
            Index = index;
            Name = name;
            Bounds = bounds;
            DisplayModes = displayModes;
        }

        public override string ToString() => $"Name: {Name ?? "Unknown"}, Bounds: {Bounds}, DisplayModes: {DisplayModes.Length}, Index: {Index}";

        public bool Equals(Display other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Index == other.Index;
        }

        /// <summary>
        /// Attempts to find a <see cref="DisplayMode"/> for the given <see cref="Display"/> that
        /// closely matches the requested parameters.
        /// </summary>
        /// <param name="size">The <see cref="Size"/> to match.</param>
        /// <param name="bitsPerPixel">The bits per pixel to match. If null, the highest available bits per pixel will be used.</param>
        /// <param name="refreshRate">The refresh rate in hertz. If null, the highest available refresh rate will be used.</param>
        public DisplayMode FindDisplayMode(Size size, int? bitsPerPixel = null, int? refreshRate = null) =>
            DisplayModes.Where(mode => mode.Size.Width <= size.Width && mode.Size.Height <= size.Height &&
                                       (bitsPerPixel == null || mode.BitsPerPixel == bitsPerPixel) &&
                                       (refreshRate == null || mode.RefreshRate == refreshRate))
                        .OrderByDescending(mode => mode.Size.Width)
                        .ThenByDescending(mode => mode.Size.Height)
                        .ThenByDescending(mode => mode.RefreshRate)
                        .ThenByDescending(mode => mode.BitsPerPixel)
                        .FirstOrDefault();
    }
}
