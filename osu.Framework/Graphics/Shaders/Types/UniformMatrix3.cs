// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 48)]
    public record struct UniformMatrix3
    {
        public UniformVector3 Row0;
        public UniformVector3 Row1;
        public UniformVector3 Row2;

        public static implicit operator Matrix3(UniformMatrix3 matrix) => new Matrix3
        {
            Row0 = matrix.Row0,
            Row1 = matrix.Row1,
            Row2 = matrix.Row2
        };

        public static implicit operator UniformMatrix3(Matrix3 matrix) => new UniformMatrix3
        {
            Row0 = matrix.Row0,
            Row1 = matrix.Row1,
            Row2 = matrix.Row2
        };
    }
}
