// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Graphics;
using System;
using System.Diagnostics.Contracts;
using Veldrid;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// A wrapper struct around <see cref="Colour4"/> that takes care of converting between sRGB and linear colour spaces.
    /// Internally this struct stores the colour in sRGB space, which is exposed by the <see cref="SRGB"/> member.
    /// This struct converts to linear space by using the <see cref="ToLinear"/> method.
    /// </summary>
    public readonly record struct SRGBColour
    {
        /// <summary>
        /// A <see cref="Colour4"/> representation of this colour in the sRGB space.
        /// </summary>
        public readonly Colour4 SRGB;

        /// <summary>
        /// A <see cref="Colour4"/> representation of this colour in the linear space.
        /// </summary>
        [Obsolete("Use ToLinear() instead.")]
        public Colour4 Linear => SRGB.ToLinear();

        /// <summary>
        /// Create an <see cref="SRGBColour"/> from four 8-bit RGBA component values, in the range 0-255.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="a">The alpha component.</param>
        public SRGBColour(byte r, byte g, byte b, byte a)
            : this(new Colour4(r, g, b, a))
        {
        }

        /// <summary>
        /// Create an <see cref="SRGBColour"/> from a <see cref="Colour4"/>, treating
        /// the contained colour components as being in gamma-corrected sRGB colour space.
        /// </summary>
        /// <param name="colour">The raw colour values to store.</param>
        public SRGBColour(Colour4 colour) => SRGB = colour;

        /// <summary>
        /// Convert this <see cref="SRGBColour"/> into a <see cref="LinearColour"/> by removing
        /// the gamma correction of the chromatic (RGB) components. The alpha component is untouched.
        /// </summary>
        /// <returns>A <see cref="LinearColour"/> struct containing the converted values.</returns>
        [Pure]
        public LinearColour ToLinear() => new LinearColour(SRGB.ToLinear());

        /// <summary>
        /// The alpha component of this colour.
        /// </summary>
        public float Alpha => SRGB.A;

        // todo: these implicit operators should be replaced with explicit static methods (https://github.com/ppy/osu-framework/issues/5714).
        public static implicit operator SRGBColour(Color4 value) => new SRGBColour(value);
        public static implicit operator Color4(SRGBColour value) => value.ToColor4();

        public static implicit operator SRGBColour(Colour4 value) => new SRGBColour(value);
        public static implicit operator Colour4(SRGBColour value) => value.SRGB;

        public static SRGBColour operator *(SRGBColour first, SRGBColour second)
        {
            if (isWhite(first))
            {
                if (first.Alpha == 1)
                    return second;

                return second.MultiplyAlpha(first.Alpha);
            }

            if (isWhite(second))
            {
                if (second.Alpha == 1)
                    return first;

                return first.MultiplyAlpha(second.Alpha);
            }

            return (first.ToLinear() * second.ToLinear()).ToSRGB();
        }

        public static SRGBColour operator *(SRGBColour first, float second)
        {
            if (second == 1)
                return first;

            return (first.ToLinear() * second).ToSRGB();
        }

        public static SRGBColour operator /(SRGBColour first, float second) => first * (1 / second);

        public static SRGBColour operator +(SRGBColour first, SRGBColour second) => (first.ToLinear() + second.ToLinear()).ToSRGB();

        public Vector4 ToVector() => new Vector4(SRGB.R, SRGB.G, SRGB.B, SRGB.A);
        public static SRGBColour FromVector(Vector4 v) => new SRGBColour(new Colour4(v.X, v.Y, v.Z, v.W));

        /// <summary>
        /// Returns a new <see cref="SRGBColour"/> with the same RGB components, but multiplying the current
        /// alpha component by a scalar value. The final alpha is clamped to the 0-1 range.
        /// </summary>
        /// <param name="scalar">The value that the existing alpha will be multiplied by.</param>
        [Pure]
        public SRGBColour MultiplyAlpha(float scalar) => new SRGBColour(SRGB.MultiplyAlpha(scalar));

        private static bool isWhite(SRGBColour colour) => colour.SRGB.R == 1 && colour.SRGB.G == 1 && colour.SRGB.B == 1;

        /// <summary>
        /// Returns a new <see cref="SRGBColour"/> with the same RGB components and a specified alpha value.
        /// The final alpha is clamped to the 0-1 range.
        /// </summary>
        /// <param name="alpha">The new alpha value for the returned colour, in the 0-1 range.</param>
        [Pure]
        public SRGBColour Opacity(float alpha) => new SRGBColour(SRGB.Opacity(alpha));

        /// <summary>
        /// Returns a lightened version of the colour.
        /// </summary>
        /// <param name="amount">Percentage light addition</param>
        [Pure]
        public SRGBColour Lighten(float amount) => new SRGBColour(SRGB.Lighten(amount));

        /// <summary>
        /// Returns a darkened version of the colour.
        /// </summary>
        /// <param name="amount">Percentage light reduction</param>
        [Pure]
        public SRGBColour Darken(float amount) => new SRGBColour(SRGB.Darken(amount));

        /// <summary>
        /// Return an <see cref="RgbaFloat"/> for interactions with Veldrid.
        /// </summary>
        /// <returns>An <see cref="RgbaFloat"/> containing the same colour values as this <see cref="SRGBColour"/>.</returns>
        [Pure]
        public RgbaFloat ToRgbaFloat() => new RgbaFloat(SRGB.Vector);

        /// <summary>
        /// Return a <see cref="Color4"/> for interactions with osuTK.
        /// </summary>
        /// <returns>A <see cref="Color4"/> containing the same colour values as this <see cref="SRGBColour"/>.</returns>
        [Pure]
        public Color4 ToColor4() => new Color4(SRGB.R, SRGB.G, SRGB.B, SRGB.A);

        public override string ToString() => $"srgb: {SRGB}, linear: {ToLinear()}";
    }
}
