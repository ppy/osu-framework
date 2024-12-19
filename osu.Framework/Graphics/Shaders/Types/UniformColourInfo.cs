// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary. Wraps <see cref="ColourInfo"/> in a <see cref="UniformMatrix4"/> structure
    /// with the rows containing top-left, bottom-left, top-right, bottom-right respectively. Alpha pre-multiplication is applied.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public record struct UniformColourInfo
    {
        public UniformMatrix4 Value;

        public static implicit operator UniformColourInfo(ColourInfo value) => new UniformColourInfo
        {
            Value =
            {
                Row0 = ((UniformColour)value.TopLeft.SRGB.ToPremultiplied()).Value,
                Row1 = ((UniformColour)value.BottomLeft.SRGB.ToPremultiplied()).Value,
                Row2 = ((UniformColour)value.TopRight.SRGB.ToPremultiplied()).Value,
                Row3 = ((UniformColour)value.BottomRight.SRGB.ToPremultiplied()).Value,
            }
        };
    }
}
