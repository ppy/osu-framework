// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using System;

namespace osu.Framework.Extensions.Color4Extensions
{
    public static class Color4Extensions
    {
        public const double GAMMA = 2.4;

        public static double ToLinear(double color)
        {
            return color <= 0.04045 ? (color / 12.92) : Math.Pow((color + 0.055) / 1.055, GAMMA);
        }

        public static double ToSRGB(double color)
        {
            return color < 0.0031308 ? (12.92 * color) : (1.055 * Math.Pow(color, 1.0 / GAMMA) - 0.055);
        }

        public static Color4 ToLinear(this Color4 colour)
        {
            return new Color4(
                (float)ToLinear(colour.R),
                (float)ToLinear(colour.G),
                (float)ToLinear(colour.B),
                colour.A);
        }

        public static Color4 ToSRGB(this Color4 colour)
        {
            return new Color4(
                (float)ToSRGB(colour.R),
                (float)ToSRGB(colour.G),
                (float)ToSRGB(colour.B),
                colour.A);
        }

        public static Color4 MultiplySRGB(Color4 first, Color4 second)
        {
            if (first.Equals(Color4.White))
                return second;
            else if (second.Equals(Color4.White))
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
            else if (second.Equals(Color4.White))
                return first;

            return new Color4(
                first.R * second.R,
                first.G * second.G,
                first.B * second.B,
                first.A * second.A);
        }
    }
}
