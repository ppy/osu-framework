// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using System;

namespace osu.Framework.Extensions.ColourExtensions
{
    public static class ColourExtensions
    {
        public const double GAMMA = 2.2;

        public static Color4 ToLinear(this Color4 colour)
        {
            return new Color4(
                (float)Math.Pow(colour.R, GAMMA),
                (float)Math.Pow(colour.G, GAMMA),
                (float)Math.Pow(colour.B, GAMMA),
                colour.A);
        }

        public static Color4 ToSRGB(this Color4 colour)
        {
            return new Color4(
                (float)Math.Pow(colour.R, 1 / GAMMA),
                (float)Math.Pow(colour.G, 1 / GAMMA),
                (float)Math.Pow(colour.B, 1 / GAMMA),
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
