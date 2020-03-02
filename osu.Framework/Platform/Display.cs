// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a physical display device on the current system.
    /// </summary>
    public sealed class Display
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

        public override string ToString() => $"Name: {Name ?? "Unknown"}, Bounds: {Bounds}, DisplayModes: {DisplayModes.Length}";
    }
}
