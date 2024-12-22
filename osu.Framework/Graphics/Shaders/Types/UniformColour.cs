// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary. Enforces colours to be in premultiplied-alpha form.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public record struct UniformColour
    {
        public UniformVector4 Value;

        public static implicit operator PremultipliedColour(UniformColour value)
            => new PremultipliedColour(value.Value.X, value.Value.Y, value.Value.Z, value.Value.W);

        public static implicit operator UniformColour(PremultipliedColour value) => new UniformColour
        {
            Value =
            {
                X = value.PremultipliedR,
                Y = value.PremultipliedG,
                Z = value.PremultipliedB,
                W = value.Occlusion,
            }
        };
    }
}
