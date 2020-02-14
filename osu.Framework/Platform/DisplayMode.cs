// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    public struct DisplayMode
    {
        public string Format;
        public Size Size;
        public int BitDepth;
        public int RefreshRate;

        public override string ToString() => $"Format: {Format ?? "Unknown"}, Size: {Size}, BitDepth: {BitDepth}, RefreshRate: {RefreshRate}";
    }
}
