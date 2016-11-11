// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.MatrixExtensions;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Primitives;
using System.Diagnostics;

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

        /// <summary>
        /// Creates a ColourInfo with a single linear colour assigned to all vertices.
        /// </summary>
        /// <param name="colour">The single linear colour to be assigned to all vertices.</param>
        public ColourInfo(SRGBColour colour)
        {
            TopLeft = BottomLeft = TopRight = BottomRight = colour;
            HasSingleColour = true;
        }

        public SRGBColour Interpolate(Vector2 interp) => SRGBColour.FromVector(
            (1 - interp.Y) * ((1 - interp.X) * TopLeft.ToVector() + interp.X * TopRight.ToVector()) +
            interp.Y * ((1 - interp.X) * BottomLeft.ToVector() + interp.X * BottomRight.ToVector()));

        public void ApplyChild(ColourInfo childColour)
        {
            Debug.Assert(HasSingleColour);
            if (childColour.HasSingleColour)
            {
                TopLeft = BottomLeft = TopRight = BottomRight = childColour.TopLeft * TopLeft;
            }
            else if (!childColour.HasSingleColour)
            {
                HasSingleColour = false;
                TopLeft = childColour.TopLeft * TopLeft;
                BottomLeft = childColour.BottomLeft * TopLeft;
                TopRight = childColour.TopRight * TopLeft;
                BottomRight = childColour.BottomRight * TopLeft;
            }
        }

        public void ApplyChild(ColourInfo childColour, Quad interp)
        {
            Debug.Assert(!HasSingleColour);

            SRGBColour newTopLeft;
            SRGBColour newTopRight;
            SRGBColour newBottomLeft;
            SRGBColour newBottomRight;

            if (childColour.HasSingleColour)
            {
                newTopLeft = Interpolate(interp.TopLeft) * childColour.TopLeft;
                newTopRight = Interpolate(interp.TopRight) * childColour.TopLeft;
                newBottomLeft = Interpolate(interp.BottomLeft) * childColour.TopLeft;
                newBottomRight = Interpolate(interp.BottomRight) * childColour.TopLeft;
            }
            else
            {
                newTopLeft = Interpolate(interp.TopLeft) * childColour.TopLeft;
                newTopRight = Interpolate(interp.TopRight) * childColour.TopRight;
                newBottomLeft = Interpolate(interp.BottomLeft) * childColour.BottomLeft;
                newBottomRight = Interpolate(interp.BottomRight) * childColour.BottomRight;
            }

            TopLeft = newTopLeft;
            TopRight = newTopRight;
            BottomLeft = newBottomLeft;
            BottomRight = newBottomRight;
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
