// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Colour;
using OpenTK.Graphics;

namespace osu.Framework.Graphics
{
    public struct DrawColourInfo : IEquatable<DrawColourInfo>
    {
        public ColourInfo Colour;
        public BlendingInfo Blending;

        public DrawColourInfo(ColourInfo? colour = null, BlendingInfo? blending = null)
        {
            Colour = colour ?? ColourInfo.SingleColour(Color4.White);
            Blending = blending ?? new BlendingInfo();
        }

        public bool Equals(DrawColourInfo other) => Colour.Equals(other.Colour) && Blending.Equals(other.Blending);
    }
}
