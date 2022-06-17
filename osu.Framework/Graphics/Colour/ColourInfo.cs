// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osuTK;
using osu.Framework.Graphics.Primitives;
using System.Diagnostics;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Colour
{
    /// <summary>
    /// ColourInfo contains information about the colours of all 4 vertices of a quad.
    /// These colours are always stored in linear space.
    /// </summary>
    public struct ColourInfo : IEquatable<ColourInfo>, IEquatable<SRGBColour>
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
        /// <returns>The created ColourInfo.</returns>
        public static ColourInfo SingleColour(SRGBColour colour)
        {
            ColourInfo result = new ColourInfo();
            result.TopLeft = result.BottomLeft = result.TopRight = result.BottomRight = colour;
            result.HasSingleColour = true;
            return result;
        }

        /// <summary>
        /// Creates a ColourInfo with a horizontal gradient.
        /// </summary>
        /// <param name="c1">The left colour of the gradient.</param>
        /// <param name="c2">The right colour of the gradient.</param>
        /// <returns>The created ColourInfo.</returns>
        public static ColourInfo GradientHorizontal(SRGBColour c1, SRGBColour c2)
        {
            ColourInfo result = new ColourInfo();
            result.TopLeft = result.BottomLeft = c1;
            result.TopRight = result.BottomRight = c2;
            result.HasSingleColour = false;
            return result;
        }

        /// <summary>
        /// Creates a ColourInfo with a vertical gradient.
        /// </summary>
        /// <param name="c1">The top colour of the gradient.</param>
        /// <param name="c2">The bottom colour of the gradient.</param>
        /// <returns>The created ColourInfo.</returns>
        public static ColourInfo GradientVertical(SRGBColour c1, SRGBColour c2)
        {
            ColourInfo result = new ColourInfo();
            result.TopLeft = result.TopRight = c1;
            result.BottomLeft = result.BottomRight = c2;
            result.HasSingleColour = false;
            return result;
        }

        private SRGBColour singleColour
        {
            readonly get
            {
                if (!HasSingleColour)
                    throw new InvalidOperationException("Attempted to read single colour from multi-colour ColourInfo.");

                return TopLeft;
            }

            set
            {
                TopLeft = BottomLeft = TopRight = BottomRight = value;
                HasSingleColour = true;
            }
        }

        public readonly SRGBColour Interpolate(Vector2 interp) => SRGBColour.FromVector(
            (1 - interp.Y) * ((1 - interp.X) * TopLeft.ToVector() + interp.X * TopRight.ToVector()) +
            interp.Y * ((1 - interp.X) * BottomLeft.ToVector() + interp.X * BottomRight.ToVector()));

        public void ApplyChild(ColourInfo childColour)
        {
            if (!HasSingleColour)
            {
                ApplyChild(childColour, new Quad(0, 0, 1, 1));
                return;
            }

            if (childColour.HasSingleColour)
                singleColour *= childColour.singleColour;
            else
            {
                HasSingleColour = false;
                BottomLeft = childColour.BottomLeft * TopLeft;
                TopRight = childColour.TopRight * TopLeft;
                BottomRight = childColour.BottomRight * TopLeft;

                // Need to assign TopLeft last to keep correctness.
                TopLeft = childColour.TopLeft * TopLeft;
            }
        }

        public void ApplyChild(ColourInfo childColour, Quad interp)
        {
            Trace.Assert(!HasSingleColour);

            SRGBColour newTopLeft = Interpolate(interp.TopLeft) * childColour.TopLeft;
            SRGBColour newTopRight = Interpolate(interp.TopRight) * childColour.TopRight;
            SRGBColour newBottomLeft = Interpolate(interp.BottomLeft) * childColour.BottomLeft;
            SRGBColour newBottomRight = Interpolate(interp.BottomRight) * childColour.BottomRight;

            TopLeft = newTopLeft;
            TopRight = newTopRight;
            BottomLeft = newBottomLeft;
            BottomRight = newBottomRight;
        }

        internal static ColourInfo Multiply(ColourInfo first, ColourInfo second) => new ColourInfo
        {
            TopLeft = first.TopLeft * second.TopLeft,
            BottomLeft = first.BottomLeft * second.BottomLeft,
            TopRight = first.TopRight * second.TopRight,
            BottomRight = first.BottomRight * second.BottomRight
        };

        /// <summary>
        /// Created a new ColourInfo with the alpha value of the colours of all vertices
        /// multiplied by a given alpha parameter.
        /// </summary>
        /// <param name="alpha">The alpha parameter to multiply the alpha values of all vertices with.</param>
        /// <returns>The new ColourInfo.</returns>
        public readonly ColourInfo MultiplyAlpha(float alpha)
        {
            if (alpha == 1.0)
                return this;

            ColourInfo result = this;
            result.TopLeft.MultiplyAlpha(alpha);

            if (HasSingleColour)
                result.BottomLeft = result.TopRight = result.BottomRight = result.TopLeft;
            else
            {
                result.BottomLeft.MultiplyAlpha(alpha);
                result.TopRight.MultiplyAlpha(alpha);
                result.BottomRight.MultiplyAlpha(alpha);
            }

            return result;
        }

        public readonly bool Equals(ColourInfo other)
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

        public readonly bool Equals(SRGBColour other) => HasSingleColour && TopLeft.Equals(other);

        /// <summary>
        /// The average colour of all corners.
        /// </summary>
        public readonly SRGBColour AverageColour
        {
            get
            {
                if (HasSingleColour)
                    return TopLeft;

                return SRGBColour.FromVector(
                    (TopLeft.ToVector() + TopRight.ToVector() + BottomLeft.ToVector() + BottomRight.ToVector()) / 4);
            }
        }

        /// <summary>
        /// The maximum alpha value of all four corners.
        /// </summary>
        public readonly float MaxAlpha
        {
            get
            {
                float max = TopLeft.Linear.A;
                if (TopRight.Linear.A > max) max = TopRight.Linear.A;
                if (BottomLeft.Linear.A > max) max = BottomLeft.Linear.A;
                if (BottomRight.Linear.A > max) max = BottomRight.Linear.A;

                return max;
            }
        }

        /// <summary>
        /// The minimum alpha value of all four corners.
        /// </summary>
        public readonly float MinAlpha
        {
            get
            {
                float min = TopLeft.Linear.A;
                if (TopRight.Linear.A < min) min = TopRight.Linear.A;
                if (BottomLeft.Linear.A < min) min = BottomLeft.Linear.A;
                if (BottomRight.Linear.A < min) min = BottomRight.Linear.A;

                return min;
            }
        }

        public override readonly string ToString() => HasSingleColour ? $@"{TopLeft} (Single)" : $@"{TopLeft}, {TopRight}, {BottomLeft}, {BottomRight}";

        public static implicit operator ColourInfo(SRGBColour colour) => SingleColour(colour);
        public static implicit operator SRGBColour(ColourInfo colour) => colour.singleColour;

        public static implicit operator ColourInfo(Color4 colour) => (SRGBColour)colour;
        public static implicit operator Color4(ColourInfo colour) => (SRGBColour)colour;

        public static implicit operator ColourInfo(Colour4 colour) => (SRGBColour)colour;
        public static implicit operator Colour4(ColourInfo colour) => (SRGBColour)colour;
    }
}
