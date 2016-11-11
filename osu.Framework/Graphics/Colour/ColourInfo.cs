// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// ColourInfo contains information about the colours of all 4 vertices of a quad.
    /// These colours are always stored in linear space.
    /// </summary>
    public struct ColourInfo : IEquatable<ColourInfo>
    {
        public SRGBColour TopLeft;
        public SRGBColour BottomLeft;
        public SRGBColour TopRight;
        public SRGBColour BottomRight;
        public bool HasSingleColour;

        public ColourInfo(ref DrawInfo parent, ColourInfo colour)
        {
            if (colour.HasSingleColour && parent.Colour.HasSingleColour)
            {
                HasSingleColour = true;
                TopLeft = BottomLeft = TopRight = BottomRight = colour.TopLeft * parent.Colour.TopLeft;
            }
            else if (!colour.HasSingleColour && parent.Colour.HasSingleColour)
            {
                HasSingleColour = false;
                TopLeft = colour.TopLeft * parent.Colour.TopLeft;
                BottomLeft = colour.BottomLeft * parent.Colour.TopLeft;
                TopRight = colour.TopRight * parent.Colour.TopLeft;
                BottomRight = colour.BottomRight * parent.Colour.TopLeft;
            }
            else
            {
                HasSingleColour = false;
                TopLeft = colour.TopLeft * parent.Colour.TopLeft;
                BottomLeft = colour.BottomLeft * parent.Colour.BottomLeft;
                TopRight = colour.TopRight * parent.Colour.TopRight;
                BottomRight = colour.BottomRight * parent.Colour.BottomRight;
            }
        }

        /// <summary>
        /// Creates a ColourInfo with a single linear colour assigned to all vertices.
        /// </summary>
        /// <param name="colour">The single linear colour to be assigned to all vertices.</param>
        public ColourInfo(SRGBColour colour)
        {
            TopLeft = BottomLeft = TopRight = BottomRight = colour;
            HasSingleColour = true;
        }

        /// <summary>
        /// Created a new ColourInfo with the alpha value of the colours of all vertices 
        /// multiplied by a given alpha parameter.
        /// </summary>
        /// <param name="alpha">The alpha parameter to multiply the alpha values of all vertices with.</param>
        /// <returns>The new ColourInfo.</returns>
        public ColourInfo MultiplyAlpha(float alpha)
        {
            if (alpha == 1.0)
                return this;

            ColourInfo result = this;
            result.TopLeft.MultiplyAlpha(alpha);

            if (!HasSingleColour)
            {
                result.BottomLeft.MultiplyAlpha(alpha);
                result.TopRight.MultiplyAlpha(alpha);
                result.BottomRight.MultiplyAlpha(alpha);
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

        public bool Equals(SRGBColour other)
        {
            return HasSingleColour && TopLeft.Equals(other);
        }
    }
}
