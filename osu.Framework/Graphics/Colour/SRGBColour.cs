// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using System;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// A wrapper struct around Color4 that takes care of converting between sRGB and linear colour spaces.
    /// Internally this struct stores the colour in sRGB space, which is exposed by the <see cref="SRGB"/> member.
    /// This struct converts to linear space by using the <see cref="Linear"/> member.
    /// </summary>
    public struct SRGBColour : IEquatable<SRGBColour>
    {
        /// <summary>
        /// A <see cref="Color4"/> representation of this colour in the sRGB space.
        /// </summary>
        public Color4 SRGB;

        /// <summary>
        /// A <see cref="Color4"/> representation of this colour in the linear space.
        /// </summary>
        public Color4 Linear => SRGB.ToLinear();

        /// <summary>
        /// The alpha component of this colour.
        /// </summary>
        public float Alpha => SRGB.A;

        // todo: these implicit operators should be replaced with explicit static methods (https://github.com/ppy/osu-framework/issues/5714).
        public static implicit operator SRGBColour(Color4 value) => new SRGBColour { SRGB = value };
        public static implicit operator Color4(SRGBColour value) => value.SRGB;

        public static implicit operator SRGBColour(Colour4 value) => new SRGBColour { SRGB = value };
        public static implicit operator Colour4(SRGBColour value) => value.SRGB;

        public static SRGBColour operator *(SRGBColour first, SRGBColour second)
        {
            var firstLinear = first.Linear;
            var secondLinear = second.Linear;

            return new SRGBColour
            {
                SRGB = new Color4(
                    firstLinear.R * secondLinear.R,
                    firstLinear.G * secondLinear.G,
                    firstLinear.B * secondLinear.B,
                    firstLinear.A * secondLinear.A).ToSRGB(),
            };
        }

        public static SRGBColour operator *(SRGBColour first, float second)
        {
            var firstLinear = first.Linear;

            return new SRGBColour
            {
                SRGB = new Color4(
                    firstLinear.R * second,
                    firstLinear.G * second,
                    firstLinear.B * second,
                    firstLinear.A * second).ToSRGB(),
            };
        }

        public static SRGBColour operator /(SRGBColour first, float second) => first * (1 / second);

        public static SRGBColour operator +(SRGBColour first, SRGBColour second)
        {
            var firstLinear = first.Linear;
            var secondLinear = second.Linear;

            return new SRGBColour
            {
                SRGB = new Color4(
                    firstLinear.R + secondLinear.R,
                    firstLinear.G + secondLinear.G,
                    firstLinear.B + secondLinear.B,
                    firstLinear.A + secondLinear.A).ToSRGB(),
            };
        }

        public readonly Vector4 ToVector() => new Vector4(SRGB.R, SRGB.G, SRGB.B, SRGB.A);
        public static SRGBColour FromVector(Vector4 v) => new SRGBColour { SRGB = new Color4(v.X, v.Y, v.Z, v.W) };

        /// <summary>
        /// Multiplies the alpha value of this colour by the given alpha factor.
        /// </summary>
        /// <param name="alpha">The alpha factor to multiply with.</param>
        public void MultiplyAlpha(float alpha) => SRGB.A *= alpha;

        public readonly bool Equals(SRGBColour other) => SRGB.Equals(other.SRGB);
        public override string ToString() => $"srgb: {SRGB}, linear: {Linear}";
    }
}
