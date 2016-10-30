// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using System;

namespace osu.Framework.Extensions.ColourExtensions
{
    public static class ColourExtensions
    {
        const double GAMMA = 2.2;

        public static Color4 toLinear(this Color4 colour)
        {
            return new Color4(
                (float)Math.Pow(colour.R, GAMMA),
                (float)Math.Pow(colour.G, GAMMA),
                (float)Math.Pow(colour.B, GAMMA),
                colour.A);
        }

        public static Color4 toSRGB(this Color4 colour)
        {
            return new Color4(
                (float)Math.Pow(colour.R, 1 / GAMMA),
                (float)Math.Pow(colour.G, 1 / GAMMA),
                (float)Math.Pow(colour.B, 1 / GAMMA),
                colour.A);
        }
    }
}
