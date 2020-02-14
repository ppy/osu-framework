// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using System.Linq;

namespace osu.Framework.Platform
{
    public struct Display
    {
        public DisplayMode[] DisplayModes;
        public Rectangle Bounds;
        public string Name;

        public override string ToString() => $"Name: {Name}, Bounds: {Bounds}" + DisplayModes.Aggregate("", (s, mode) => $"{s}\n-- {mode}");
    }
}
