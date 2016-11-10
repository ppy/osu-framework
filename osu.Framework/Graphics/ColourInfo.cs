// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.ColourExtensions;

namespace osu.Framework.Graphics
{
    public struct ColourInfo : IEquatable<ColourInfo>
    {
        public Color4 TopLeft;
        public Color4 BottomLeft;
        public Color4 TopRight;
        public Color4 BottomRight;
        public bool HasSingleColour;

        public ColourInfo(ref DrawInfo parent, ColourInfo colour)
        {
            if (colour.HasSingleColour && parent.Colour.HasSingleColour)
            {
                HasSingleColour = true;
                TopLeft = BottomLeft = TopRight = BottomRight = ColourExtensions.MultiplySRGB(colour.TopLeft, parent.Colour.TopLeft);
            }
            else if (!colour.HasSingleColour && parent.Colour.HasSingleColour)
            {
                HasSingleColour = false;
                TopLeft = ColourExtensions.MultiplySRGB(colour.TopLeft, parent.Colour.TopLeft);
                BottomLeft = ColourExtensions.MultiplySRGB(colour.BottomLeft, parent.Colour.TopLeft);
                TopRight = ColourExtensions.MultiplySRGB(colour.TopRight, parent.Colour.TopLeft);
                BottomRight = ColourExtensions.MultiplySRGB(colour.BottomRight, parent.Colour.TopLeft);
            }
            else
            {
                HasSingleColour = false;
                TopLeft = ColourExtensions.MultiplySRGB(colour.TopLeft, parent.Colour.TopLeft);
                BottomLeft = ColourExtensions.MultiplySRGB(colour.BottomLeft, parent.Colour.BottomLeft);
                TopRight = ColourExtensions.MultiplySRGB(colour.TopRight, parent.Colour.TopRight);
                BottomRight = ColourExtensions.MultiplySRGB(colour.BottomRight, parent.Colour.BottomRight);
            }
        }

        public ColourInfo(Color4 colour)
        {
            TopLeft = BottomLeft = TopRight = BottomRight = colour;
            HasSingleColour = true;
        }

        public ColourInfo MultiplyAlpha(float alpha)
        {
            if (alpha == 1.0)
                return this;

            ColourInfo result = this;
            result.TopLeft.A *= alpha;

            if (!HasSingleColour)
            {
                result.BottomLeft.A *= alpha;
                result.TopRight.A *= alpha;
                result.BottomRight.A *= alpha;
            }

            return result;
        }

        public bool Equals(ColourInfo other)
        {
            if (!HasSingleColour)
            {
                if (other.HasSingleColour)
                    return false;

                return
                    TopLeft.Equals(other.TopLeft) &&
                    TopRight.Equals(other.TopRight) &&
                    BottomLeft.Equals(other.BottomLeft) &&
                    BottomRight.Equals(other.BottomRight);
            }

            return other.HasSingleColour && TopLeft.Equals(other.TopLeft);
        }

        public bool Equals(Color4 other)
        {
            return HasSingleColour && TopLeft.Equals(other);
        }
    }
}
