// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 4-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public record struct UniformInt
    {
        public int Value;

        public static implicit operator int(UniformInt value) => value.Value;
        public static implicit operator UniformInt(int value) => new UniformInt { Value = value };
    }
}
