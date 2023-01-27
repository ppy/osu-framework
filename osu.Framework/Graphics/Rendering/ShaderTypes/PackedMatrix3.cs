// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Rendering.ShaderTypes
{
    [StructLayout(LayoutKind.Explicit, Size = 48)] // Align: N
    public record struct PackedMatrix3
    {
        [FieldOffset(0)] // Align: 4N
        public Vector3 Row0;

        [FieldOffset(16)] // Align: 4N
        public Vector3 Row1;

        [FieldOffset(32)] // Align: 4N
        public Vector3 Row2;

        public static implicit operator PackedMatrix3(Matrix3 matrix) => new PackedMatrix3
        {
            Row0 = matrix.Row0,
            Row1 = matrix.Row1,
            Row2 = matrix.Row2
        };
    }
}
