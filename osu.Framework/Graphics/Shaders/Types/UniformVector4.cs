// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public record struct UniformVector4
    {
        public UniformFloat X;
        public UniformFloat Y;
        public UniformFloat Z;
        public UniformFloat W;

        public static implicit operator Vector4(UniformVector4 value) => new Vector4
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
            W = value.W
        };

        public static implicit operator UniformVector4(Vector4 value) => new UniformVector4
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
            W = value.W
        };
    }
}
