// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK.Graphics;
using System;
using System.Globalization;

namespace osu.Framework.Extensions.Color4Extensions
{
    public static class Color4Extensions
    {
        public const double GAMMA = 2.4;

        public static double ToLinear(double color) => color <= 0.04045 ? color / 12.92 : Math.Pow((color + 0.055) / 1.055, GAMMA);

        public static double ToSRGB(double color) => color < 0.0031308 ? 12.92 * color : 1.055 * Math.Pow(color, 1.0 / GAMMA) - 0.055;

        public static Color4 Opacity(this Color4 color, float a) => new Color4(color.R, color.G, color.B, a);

        public static Color4 Opacity(this Color4 color, byte a) => new Color4(color.R, color.G, color.B, a / 255f);

        public static Color4 ToLinear(this Color4 colour) =>
            new Color4(
                (float)ToLinear(colour.R),
                (float)ToLinear(colour.G),
                (float)ToLinear(colour.B),
                colour.A);

        public static Color4 ToSRGB(this Color4 colour) =>
            new Color4(
                (float)ToSRGB(colour.R),
                (float)ToSRGB(colour.G),
                (float)ToSRGB(colour.B),
                colour.A);

        public static Color4 MultiplySRGB(Color4 first, Color4 second)
        {
            if (first.Equals(Color4.White))
                return second;

            if (second.Equals(Color4.White))
                return first;

            first = first.ToLinear();
            second = second.ToLinear();

            return new Color4(
                first.R * second.R,
                first.G * second.G,
                first.B * second.B,
                first.A * second.A).ToSRGB();
        }

        public static Color4 Multiply(Color4 first, Color4 second)
        {
            if (first.Equals(Color4.White))
                return second;

            if (second.Equals(Color4.White))
                return first;

            return new Color4(
                first.R * second.R,
                first.G * second.G,
                first.B * second.B,
                first.A * second.A);
        }

        /// <summary>
        /// Returns a lightened version of the colour.
        /// </summary>
        /// <param name="colour">Original colour</param>
        /// <param name="amount">Decimal light addition</param>
        public static Color4 Lighten(this Color4 colour, float amount) => Multiply(colour, 1 + amount);

        /// <summary>
        /// Returns a darkened version of the colour.
        /// </summary>
        /// <param name="colour">Original colour</param>
        /// <param name="amount">Percentage light reduction</param>
        public static Color4 Darken(this Color4 colour, float amount) => Multiply(colour, 1 / (1 + amount));

        /// <summary>
        /// Multiply the RGB coordinates by a scalar.
        /// </summary>
        /// <param name="colour">Original colour</param>
        /// <param name="scalar">A scalar to multiply with</param>
        public static Color4 Multiply(this Color4 colour, float scalar)
        {
            if (scalar < 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Can not multiply colours by negative values.");

            return new Color4(
                Math.Min(1, colour.R * scalar),
                Math.Min(1, colour.G * scalar),
                Math.Min(1, colour.B * scalar),
                colour.A);
        }

        /// <summary>
        /// Converts an RGB or RGBA-formatted hex colour code into a <see cref="Color4"/>.
        /// Supported colour code formats:
        /// <list type="bullet">
        /// <item><description>RGB</description></item>
        /// <item><description>#RGB</description></item>
        /// <item><description>RGBA</description></item>
        /// <item><description>#RGBA</description></item>
        /// <item><description>RRGGBB</description></item>
        /// <item><description>#RRGGBB</description></item>
        /// <item><description>RRGGBBAA</description></item>
        /// <item><description>#RRGGBBAA</description></item>
        /// </list>
        /// </summary>
        /// <param name="hex">The hex code.</param>
        /// <returns>The <see cref="Color4"/> representing the colour.</returns>
        /// <exception cref="ArgumentException">If <paramref name="hex"/> is not a supported colour code.</exception>
        public static Color4 FromHex(string hex)
        {
            var hexSpan = hex[0] == '#' ? hex.AsSpan().Slice(1) : hex.AsSpan();

            switch (hexSpan.Length)
            {
                default:
                    throw new ArgumentException(@"Invalid hex string length!");

                case 3:
                    return new Color4(
                        (byte)(byte.Parse(hexSpan.Slice(0, 1), NumberStyles.HexNumber) * 17),
                        (byte)(byte.Parse(hexSpan.Slice(1, 1), NumberStyles.HexNumber) * 17),
                        (byte)(byte.Parse(hexSpan.Slice(2, 1), NumberStyles.HexNumber) * 17),
                        255);

                case 6:
                    return new Color4(
                        byte.Parse(hexSpan.Slice(0, 2), NumberStyles.HexNumber),
                        byte.Parse(hexSpan.Slice(2, 2), NumberStyles.HexNumber),
                        byte.Parse(hexSpan.Slice(4, 2), NumberStyles.HexNumber),
                        255);

                case 4:
                    return new Color4(
                        (byte)(byte.Parse(hexSpan.Slice(0, 1), NumberStyles.HexNumber) * 17),
                        (byte)(byte.Parse(hexSpan.Slice(1, 1), NumberStyles.HexNumber) * 17),
                        (byte)(byte.Parse(hexSpan.Slice(2, 1), NumberStyles.HexNumber) * 17),
                        (byte)(byte.Parse(hexSpan.Slice(3, 1), NumberStyles.HexNumber) * 17));

                case 8:
                    return new Color4(
                        byte.Parse(hexSpan.Slice(0, 2), NumberStyles.HexNumber),
                        byte.Parse(hexSpan.Slice(2, 2), NumberStyles.HexNumber),
                        byte.Parse(hexSpan.Slice(4, 2), NumberStyles.HexNumber),
                        byte.Parse(hexSpan.Slice(6, 2), NumberStyles.HexNumber));
            }
        }

        /// <summary>
        /// Converts a <see cref="Color4"/> into a hex colour code.
        /// </summary>
        /// <param name="colour">The <see cref="Color4"/> to convert.</param>
        /// <param name="alwaysOutputAlpha">Whether the alpha channel should always be output. If <c>false</c>, the alpha channel is only output if <paramref name="colour"/> is translucent.</param>
        /// <returns>The hex code representing the colour.</returns>
        public static string ToHex(this Color4 colour, bool alwaysOutputAlpha = false)
        {
            int argb = colour.ToArgb();
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;

            if (!alwaysOutputAlpha && a == 255)
                return $"#{r:X2}{g:X2}{b:X2}";

            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        /// <summary>
        /// Converts an HSV colour to a <see cref="Color4"/>.
        /// </summary>
        /// <param name="h">The hue, between 0 and 360.</param>
        /// <param name="s">The saturation, between 0 and 1.</param>
        /// <param name="v">The value, between 0 and 1.</param>
        public static Color4 FromHSV(float h, float s, float v)
        {
            if (h < 0 || h > 360)
                throw new ArgumentOutOfRangeException(nameof(h), "Hue must be between 0 and 360.");

            int hi = ((int)(h / 60.0f)) % 6;
            float f = h / 60.0f - (int)(h / 60.0);
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (hi)
            {
                case 0:
                    return toColor4(v, t, p);

                case 1:
                    return toColor4(q, v, p);

                case 2:
                    return toColor4(p, v, t);

                case 3:
                    return toColor4(p, q, v);

                case 4:
                    return toColor4(t, p, v);

                case 5:
                    return toColor4(v, p, q);

                default:
                    throw new ArgumentOutOfRangeException(nameof(h), "Hue is out of range.");
            }

            static Color4 toColor4(float fr, float fg, float fb)
            {
                byte r = (byte)Math.Clamp(fr * 255, 0, 255);
                byte g = (byte)Math.Clamp(fg * 255, 0, 255);
                byte b = (byte)Math.Clamp(fb * 255, 0, 255);
                return new Color4(r, g, b, 255);
            }
        }

        /// <summary>
        /// Converts a <see cref="Color4"/> to an HSV colour.
        /// </summary>
        /// <param name="colour">The <see cref="Color4"/> to convert.</param>
        /// <returns>The HSV colour.</returns>
        public static (float h, float s, float v) ToHSV(this Color4 colour)
        {
            float h;
            float s;
            float r = colour.R;
            float g = colour.G;
            float b = colour.B;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            if (max == min)
                h = 0;
            else if (max == r)
                h = (60 * (g - b) / (max - min) + 360) % 360;
            else if (max == g)
                h = 60 * (b - r) / (max - min) + 120;
            else
                h = 60 * (r - g) / (max - min) + 240;

            if (max == 0)
                s = 0;
            else
                s = (max - min) / max;

            float v = max;

            return (h, s, v);
        }
    }
}
