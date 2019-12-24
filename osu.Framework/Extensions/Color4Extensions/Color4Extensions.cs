// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;
using System;
using System.Linq;

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
        /// <returns></returns>
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

        public static Color4 FromHex(string hex)
        {
            if (hex[0] == '#')
                hex = hex.Substring(1);

            switch (hex.Length)
            {
                default:
                    throw new ArgumentException(@"Invalid hex string length!");

                case 3:
                    return new Color4(
                        (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17),
                        (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17),
                        (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17),
                        255);

                case 6:
                    return new Color4(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16),
                        255);
            }
        }

        public static string ToHex(this Color4 color, bool forceOutputAlpha = false)
        {
            var argb = color.ToArgb();
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)(argb >> 0);

            if(!forceOutputAlpha && a == 255)
                return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b).ToLower();

            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a).ToLower();
        }

        /// <summary>
        /// Convert HSV to Color4
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Color4 ToRGB(float h, float s, float v)
        {
            int Hi = ((int)(h / 60.0)) % 6;
            float f = h / 60.0f - (int)(h / 60.0);
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (Hi)
            {
                case 0:
                    return fromRGB(v, t, p);
                case 1:
                    return fromRGB(q, v, p);
                case 2:
                    return fromRGB(p, v, t);
                case 3:
                    return fromRGB(p, q, v);
                case 4:
                    return fromRGB(t, p, v);
                case 5:
                    return fromRGB(v, p, q);
            }

            // Should not goes to here
            throw new InvalidOperationException();

            static Color4 fromRGB(float fr, float fg, float fb)
            {
                fr *= 255;
                fg *= 255;
                fb *= 255;
                byte r = (byte)((fr < 0) ? 0 : (fr > 255) ? 255 : fr);
                byte g = (byte)((fg < 0) ? 0 : (fg > 255) ? 255 : fg);
                byte b = (byte)((fb < 0) ? 0 : (fb > 255) ? 255 : fb);
                return new Color4(r, g, b, 255);
            }
        }

        /// <summary>
        /// Convert color4 to HSV
        /// </summary>
        /// <param name="c">Color4</param>
        /// <param name="h">H value, between 0 to 360</param>
        /// <param name="s">S value, between 0 to 1</param>
        /// <param name="v">V value, between 0 to 1</param>
        public static void ToHSV(Color4 c, out float h, out float s, out float v)
        {
            float r = c.R;
            float g = c.G;
            float b = c.B;

            var list = new float[] { r, g, b };
            var max = list.Max();
            var min = list.Min();

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

            v = max;
        }
    }
}
