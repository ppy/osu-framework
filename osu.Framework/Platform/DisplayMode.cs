// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;

namespace osu.Framework.Platform
{
    public struct DisplayMode
    {
        public Size Size;
        public int RefreshRate;
        public int BitDepth;
        public string Name;

        public override string ToString() => $"Name: {Name}, Size: {Size}, BitDepth: {BitDepth}, RefreshRate: {RefreshRate}";
    }
}
