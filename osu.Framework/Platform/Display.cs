// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    public struct Display
    {
        public string Name;
        public Rectangle Bounds;
        public DisplayMode[] DisplayModes;

        public override string ToString() => $"Name: {Name ?? "Unknown"}, Bounds: {Bounds}, DisplayModes: {DisplayModes.Length}";
    }
}
