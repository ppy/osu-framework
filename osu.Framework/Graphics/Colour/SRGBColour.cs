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

        #region Constants

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static SRGBColour Transparent => new SRGBColour(255, 255, 255, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static SRGBColour AliceBlue => new SRGBColour(240, 248, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static SRGBColour AntiqueWhite => new SRGBColour(250, 235, 215, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static SRGBColour Aqua => new SRGBColour(0, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static SRGBColour Aquamarine => new SRGBColour(127, 255, 212, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static SRGBColour Azure => new SRGBColour(240, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static SRGBColour Beige => new SRGBColour(245, 245, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static SRGBColour Bisque => new SRGBColour(255, 228, 196, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static SRGBColour Black => new SRGBColour(0, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static SRGBColour BlanchedAlmond => new SRGBColour(255, 235, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static SRGBColour Blue => new SRGBColour(0, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static SRGBColour BlueViolet => new SRGBColour(138, 43, 226, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static SRGBColour Brown => new SRGBColour(165, 42, 42, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static SRGBColour BurlyWood => new SRGBColour(222, 184, 135, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static SRGBColour CadetBlue => new SRGBColour(95, 158, 160, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static SRGBColour Chartreuse => new SRGBColour(127, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static SRGBColour Chocolate => new SRGBColour(210, 105, 30, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static SRGBColour Coral => new SRGBColour(255, 127, 80, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static SRGBColour CornflowerBlue => new SRGBColour(100, 149, 237, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static SRGBColour Cornsilk => new SRGBColour(255, 248, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static SRGBColour Crimson => new SRGBColour(220, 20, 60, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static SRGBColour Cyan => new SRGBColour(0, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static SRGBColour DarkBlue => new SRGBColour(0, 0, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static SRGBColour DarkCyan => new SRGBColour(0, 139, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static SRGBColour DarkGoldenrod => new SRGBColour(184, 134, 11, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static SRGBColour DarkGray => new SRGBColour(169, 169, 169, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static SRGBColour DarkGreen => new SRGBColour(0, 100, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static SRGBColour DarkKhaki => new SRGBColour(189, 183, 107, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static SRGBColour DarkMagenta => new SRGBColour(139, 0, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static SRGBColour DarkOliveGreen => new SRGBColour(85, 107, 47, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static SRGBColour DarkOrange => new SRGBColour(255, 140, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static SRGBColour DarkOrchid => new SRGBColour(153, 50, 204, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static SRGBColour DarkRed => new SRGBColour(139, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static SRGBColour DarkSalmon => new SRGBColour(233, 150, 122, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139, 255).
        /// </summary>
        public static SRGBColour DarkSeaGreen => new SRGBColour(143, 188, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static SRGBColour DarkSlateBlue => new SRGBColour(72, 61, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static SRGBColour DarkSlateGray => new SRGBColour(47, 79, 79, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static SRGBColour DarkTurquoise => new SRGBColour(0, 206, 209, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static SRGBColour DarkViolet => new SRGBColour(148, 0, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static SRGBColour DeepPink => new SRGBColour(255, 20, 147, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static SRGBColour DeepSkyBlue => new SRGBColour(0, 191, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static SRGBColour DimGray => new SRGBColour(105, 105, 105, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static SRGBColour DodgerBlue => new SRGBColour(30, 144, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static SRGBColour Firebrick => new SRGBColour(178, 34, 34, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static SRGBColour FloralWhite => new SRGBColour(255, 250, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static SRGBColour ForestGreen => new SRGBColour(34, 139, 34, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static SRGBColour Fuchsia => new SRGBColour(255, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static SRGBColour Gainsboro => new SRGBColour(220, 220, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static SRGBColour GhostWhite => new SRGBColour(248, 248, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static SRGBColour Gold => new SRGBColour(255, 215, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static SRGBColour Goldenrod => new SRGBColour(218, 165, 32, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static SRGBColour Gray => new SRGBColour(128, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static SRGBColour Green => new SRGBColour(0, 128, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static SRGBColour GreenYellow => new SRGBColour(173, 255, 47, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static SRGBColour Honeydew => new SRGBColour(240, 255, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static SRGBColour HotPink => new SRGBColour(255, 105, 180, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static SRGBColour IndianRed => new SRGBColour(205, 92, 92, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static SRGBColour Indigo => new SRGBColour(75, 0, 130, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static SRGBColour Ivory => new SRGBColour(255, 255, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static SRGBColour Khaki => new SRGBColour(240, 230, 140, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static SRGBColour Lavender => new SRGBColour(230, 230, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static SRGBColour LavenderBlush => new SRGBColour(255, 240, 245, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static SRGBColour LawnGreen => new SRGBColour(124, 252, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static SRGBColour LemonChiffon => new SRGBColour(255, 250, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static SRGBColour LightBlue => new SRGBColour(173, 216, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static SRGBColour LightCoral => new SRGBColour(240, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static SRGBColour LightCyan => new SRGBColour(224, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static SRGBColour LightGoldenrodYellow => new SRGBColour(250, 250, 210, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static SRGBColour LightGreen => new SRGBColour(144, 238, 144, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static SRGBColour LightGray => new SRGBColour(211, 211, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static SRGBColour LightPink => new SRGBColour(255, 182, 193, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static SRGBColour LightSalmon => new SRGBColour(255, 160, 122, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static SRGBColour LightSeaGreen => new SRGBColour(32, 178, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static SRGBColour LightSkyBlue => new SRGBColour(135, 206, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static SRGBColour LightSlateGray => new SRGBColour(119, 136, 153, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static SRGBColour LightSteelBlue => new SRGBColour(176, 196, 222, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static SRGBColour LightYellow => new SRGBColour(255, 255, 224, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static SRGBColour Lime => new SRGBColour(0, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static SRGBColour LimeGreen => new SRGBColour(50, 205, 50, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static SRGBColour Linen => new SRGBColour(250, 240, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static SRGBColour Magenta => new SRGBColour(255, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static SRGBColour Maroon => new SRGBColour(128, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static SRGBColour MediumAquamarine => new SRGBColour(102, 205, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static SRGBColour MediumBlue => new SRGBColour(0, 0, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static SRGBColour MediumOrchid => new SRGBColour(186, 85, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static SRGBColour MediumPurple => new SRGBColour(147, 112, 219, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static SRGBColour MediumSeaGreen => new SRGBColour(60, 179, 113, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static SRGBColour MediumSlateBlue => new SRGBColour(123, 104, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static SRGBColour MediumSpringGreen => new SRGBColour(0, 250, 154, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static SRGBColour MediumTurquoise => new SRGBColour(72, 209, 204, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static SRGBColour MediumVioletRed => new SRGBColour(199, 21, 133, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static SRGBColour MidnightBlue => new SRGBColour(25, 25, 112, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static SRGBColour MintCream => new SRGBColour(245, 255, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static SRGBColour MistyRose => new SRGBColour(255, 228, 225, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static SRGBColour Moccasin => new SRGBColour(255, 228, 181, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static SRGBColour NavajoWhite => new SRGBColour(255, 222, 173, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static SRGBColour Navy => new SRGBColour(0, 0, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static SRGBColour OldLace => new SRGBColour(253, 245, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static SRGBColour Olive => new SRGBColour(128, 128, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static SRGBColour OliveDrab => new SRGBColour(107, 142, 35, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static SRGBColour Orange => new SRGBColour(255, 165, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static SRGBColour OrangeRed => new SRGBColour(255, 69, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static SRGBColour Orchid => new SRGBColour(218, 112, 214, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static SRGBColour PaleGoldenrod => new SRGBColour(238, 232, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static SRGBColour PaleGreen => new SRGBColour(152, 251, 152, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static SRGBColour PaleTurquoise => new SRGBColour(175, 238, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static SRGBColour PaleVioletRed => new SRGBColour(219, 112, 147, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static SRGBColour PapayaWhip => new SRGBColour(255, 239, 213, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static SRGBColour PeachPuff => new SRGBColour(255, 218, 185, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static SRGBColour Peru => new SRGBColour(205, 133, 63, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static SRGBColour Pink => new SRGBColour(255, 192, 203, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static SRGBColour Plum => new SRGBColour(221, 160, 221, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static SRGBColour PowderBlue => new SRGBColour(176, 224, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static SRGBColour Purple => new SRGBColour(128, 0, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static SRGBColour Red => new SRGBColour(255, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static SRGBColour RosyBrown => new SRGBColour(188, 143, 143, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static SRGBColour RoyalBlue => new SRGBColour(65, 105, 225, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static SRGBColour SaddleBrown => new SRGBColour(139, 69, 19, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static SRGBColour Salmon => new SRGBColour(250, 128, 114, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static SRGBColour SandyBrown => new SRGBColour(244, 164, 96, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static SRGBColour SeaGreen => new SRGBColour(46, 139, 87, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static SRGBColour SeaShell => new SRGBColour(255, 245, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static SRGBColour Sienna => new SRGBColour(160, 82, 45, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static SRGBColour Silver => new SRGBColour(192, 192, 192, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static SRGBColour SkyBlue => new SRGBColour(135, 206, 235, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static SRGBColour SlateBlue => new SRGBColour(106, 90, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static SRGBColour SlateGray => new SRGBColour(112, 128, 144, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static SRGBColour Snow => new SRGBColour(255, 250, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static SRGBColour SpringGreen => new SRGBColour(0, 255, 127, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static SRGBColour SteelBlue => new SRGBColour(70, 130, 180, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static SRGBColour Tan => new SRGBColour(210, 180, 140, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static SRGBColour Teal => new SRGBColour(0, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static SRGBColour Thistle => new SRGBColour(216, 191, 216, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static SRGBColour Tomato => new SRGBColour(255, 99, 71, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static SRGBColour Turquoise => new SRGBColour(64, 224, 208, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static SRGBColour Violet => new SRGBColour(238, 130, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static SRGBColour Wheat => new SRGBColour(245, 222, 179, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static SRGBColour White => new SRGBColour(255, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static SRGBColour WhiteSmoke => new SRGBColour(245, 245, 245, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static SRGBColour Yellow => new SRGBColour(255, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static SRGBColour YellowGreen => new SRGBColour(154, 205, 50, 255);

        #endregion
    }
}
