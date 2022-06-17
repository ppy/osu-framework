// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osuTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using System;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// A wrapper struct around Color4 that takes care of converting between sRGB and linear colour spaces.
    /// Internally this struct stores the colour in linear space, which is exposed by the Linear member.
    /// This struct implicitly converts to sRGB space Color4 values (i.e. it can be assigned and implicitly cast)
    /// to sRGB Color4.
    /// </summary>
    public struct SRGBColour : IEquatable<SRGBColour>
    {
        public Color4 Linear;

        public static implicit operator SRGBColour(Color4 value) => new SRGBColour { Linear = value.ToLinear() };
        public static implicit operator Color4(SRGBColour value) => value.Linear.ToSRGB();

        public static implicit operator SRGBColour(Colour4 value) => new SRGBColour { Linear = value.ToLinear() };
        public static implicit operator Colour4(SRGBColour value) => value.Linear.ToSRGB();

        /// <summary>
        /// Multiplies 2 colours in linear colour space.
        /// </summary>
        /// <param name="first">First factor.</param>
        /// <param name="second">Second factor.</param>
        /// <returns>Product of first and second.</returns>
        public static SRGBColour operator *(SRGBColour first, SRGBColour second) => new SRGBColour
        {
            Linear = new Color4(
                first.Linear.R * second.Linear.R,
                first.Linear.G * second.Linear.G,
                first.Linear.B * second.Linear.B,
                first.Linear.A * second.Linear.A),
        };

        public static SRGBColour operator *(SRGBColour first, float second) => new SRGBColour
        {
            Linear = new Color4(
                first.Linear.R * second,
                first.Linear.G * second,
                first.Linear.B * second,
                first.Linear.A * second),
        };

        public static SRGBColour operator /(SRGBColour first, float second) => first * (1 / second);

        public static SRGBColour operator +(SRGBColour first, SRGBColour second) => new SRGBColour
        {
            Linear = new Color4(
                first.Linear.R + second.Linear.R,
                first.Linear.G + second.Linear.G,
                first.Linear.B + second.Linear.B,
                first.Linear.A + second.Linear.A),
        };

        public readonly Vector4 ToVector() => new Vector4(Linear.R, Linear.G, Linear.B, Linear.A);
        public static SRGBColour FromVector(Vector4 v) => new SRGBColour { Linear = new Color4(v.X, v.Y, v.Z, v.W) };

        /// <summary>
        /// Multiplies the alpha value of this colour by the given alpha factor.
        /// </summary>
        /// <param name="alpha">The alpha factor to multiply with.</param>
        public void MultiplyAlpha(float alpha) => Linear.A *= alpha;

        public readonly bool Equals(SRGBColour other) => Linear.Equals(other.Linear);
        public override readonly string ToString() => Linear.ToString();
    }
}
