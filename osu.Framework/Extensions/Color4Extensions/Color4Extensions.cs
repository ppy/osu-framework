// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;
using System;

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
        /// HSL to RGB conversion.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="sl">The sl.</param>
        /// <param name="l">The l.</param>
        /// <returns></returns>
        public static Color4 HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;
            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            return new Color4(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f), 255);
        }

        /// <summary>
        /// RGB to HSL conversion
        /// Given a Color (RGB Struct) in range of 0-255
        /// Return H,S,L in range of 0-1
        /// </summary>
        /// <param name="rgb">The RGB.</param>
        /// <param name="h">The h.</param>
        /// <param name="s">The s.</param>
        /// <param name="l">The l.</param>
        public static void RGB2HSL(Color4 rgb, out double h, out double s, out double l)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double v;
            double m;
            double vm;
            double r2, g2, b2;

            h = 0; // default to black
            s = 0;
            l = 0;
            v = Math.Max(r, g);
            v = Math.Max(v, b);
            m = Math.Min(r, g);
            m = Math.Min(m, b);

            l = (m + v) / 2.0;
            if (l <= 0.0)
            {
                return;
            }
            vm = v - m;
            s = vm;
            if (s > 0.0)
            {
                s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
            }
            else
            {
                return;
            }
            r2 = (v - r) / vm;
            g2 = (v - g) / vm;
            b2 = (v - b) / vm;
            if (r == v)
            {
                h = (g == m ? 5.0 + b2 : 1.0 - g2);
            }
            else if (g == v)
            {
                h = (b == m ? 1.0 + r2 : 3.0 - b2);
            }
            else
            {
                h = (r == m ? 3.0 + g2 : 5.0 - r2);
            }
            h /= 6.0;
        }
    }
}
