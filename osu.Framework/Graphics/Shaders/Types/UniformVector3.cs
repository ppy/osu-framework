// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary. Is equivalent to a <see cref="UniformVector4"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public record struct UniformVector3
    {
        public UniformVector4 Value;

        public static implicit operator Vector3(UniformVector3 value) => new Vector3
        {
            X = value.Value.X,
            Y = value.Value.Y,
            Z = value.Value.Z
        };

        public static implicit operator UniformVector3(Vector3 value) => new UniformVector3
        {
            Value =
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z
            }
        };
    }
}
