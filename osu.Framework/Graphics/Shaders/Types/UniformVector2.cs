// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to an 8-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public record struct UniformVector2
    {
        public UniformFloat X;
        public UniformFloat Y;

        public static implicit operator Vector2(UniformVector2 value) => new Vector2
        {
            X = value.X,
            Y = value.Y
        };

        public static implicit operator UniformVector2(Vector2 value) => new UniformVector2
        {
            X = value.X,
            Y = value.Y
        };
    }
}
