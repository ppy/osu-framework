// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;
using osu.Framework.Graphics.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
                Debug.Assert(HasSingleColour);
                return TopLeft;
            }
            set
            {
                TopLeft = BottomLeft = TopRight = BottomRight = value;
                HasSingleColour = true;
            }
        }

        /// <summary>
        /// Attempts to extract the single colour represented by this <see cref="ColourInfo"/>.
        /// </summary>
        /// <param name="colour">The extracted colour. If <c>false</c> is returned, this represents the top-left colour.</param>
        /// <returns>Whether the extracted colour is the single colour represented by this <see cref="ColourInfo"/>.</returns>
        public readonly bool TryExtractSingleColour(out SRGBColour colour)
        {
            // To make this code branchless, we have to work around the assertion in singleColour.
            colour = TopLeft;
            return HasSingleColour;
        }

        public readonly SRGBColour Interpolate(Vector2 interp) => SRGBColour.FromVector(
            (1 - interp.Y) * ((1 - interp.X) * TopLeft.ToVector() + interp.X * TopRight.ToVector()) +
            interp.Y * ((1 - interp.X) * BottomLeft.ToVector() + interp.X * BottomRight.ToVector()));

        /// <summary>
        /// Interpolates this <see cref="ColourInfo"/> across a quad.
        /// </summary>
        /// <remarks>
        /// This method is especially useful when working with multi-colour <see cref="ColourInfo"/>s.
        /// When such a colour is interpolated across a quad that is a subset of the unit quad (0, 0, 1, 1),
        /// the resulting colour can be thought of as the the original colour but "cropped" to the bounds of the subquad.
        /// </remarks>
        public readonly ColourInfo Interpolate(Quad quad)
        {
            if (HasSingleColour)
                return this;

            return new ColourInfo
            {
                TopLeft = Interpolate(quad.TopLeft),
                TopRight = Interpolate(quad.TopRight),
                BottomLeft = Interpolate(quad.BottomLeft),
                BottomRight = Interpolate(quad.BottomRight),
                HasSingleColour = false
            };
        }

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

            if (TryExtractSingleColour(out SRGBColour single))
            {
                single.MultiplyAlpha(alpha);
                return single;
            }

            ColourInfo result = this;
            result.TopLeft.MultiplyAlpha(alpha);
            result.BottomLeft.MultiplyAlpha(alpha);
            result.TopRight.MultiplyAlpha(alpha);
            result.BottomRight.MultiplyAlpha(alpha);

            return result;
        }

        public readonly ColourInfo MultiplyAlpha(ColourInfo other)
        {
            if (other.HasSingleColour && other.singleColour.Alpha == 1.0)
                return this;

            if (TryExtractSingleColour(out SRGBColour single) && other.TryExtractSingleColour(out SRGBColour alphaSingle))
            {
                single.MultiplyAlpha(alphaSingle.Alpha);
                return single;
            }

            ColourInfo result = this;
            result.TopLeft.MultiplyAlpha(other.TopLeft.Alpha);
            result.BottomLeft.MultiplyAlpha(other.BottomLeft.Alpha);
            result.TopRight.MultiplyAlpha(other.TopRight.Alpha);
            result.BottomRight.MultiplyAlpha(other.BottomRight.Alpha);

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

            return other.HasSingleColour && singleColour.Equals(other.singleColour);
        }

        public readonly bool Equals(SRGBColour other) => HasSingleColour && singleColour.Equals(other);

        /// <summary>
        /// The average colour of all corners.
        /// </summary>
        public readonly SRGBColour AverageColour
        {
            get
            {
                if (HasSingleColour)
                    return singleColour;

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
                float max = TopLeft.Alpha;
                if (TopRight.Alpha > max) max = TopRight.Alpha;
                if (BottomLeft.Alpha > max) max = BottomLeft.Alpha;
                if (BottomRight.Alpha > max) max = BottomRight.Alpha;

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
                float min = TopLeft.Alpha;
                if (TopRight.Alpha < min) min = TopRight.Alpha;
                if (BottomLeft.Alpha < min) min = BottomLeft.Alpha;
                if (BottomRight.Alpha < min) min = BottomRight.Alpha;

                return min;
            }
        }

        public override readonly string ToString() => HasSingleColour ? $@"{TopLeft} (Single)" : $@"{TopLeft}, {TopRight}, {BottomLeft}, {BottomRight}";

        public static implicit operator ColourInfo(SRGBColour colour) => SingleColour(colour);

        public static implicit operator SRGBColour(ColourInfo colour)
        {
            if (!colour.HasSingleColour)
                throwConversionFromMultiColourToSingleColourException();

            return colour.singleColour;

            [DoesNotReturn]
            static void throwConversionFromMultiColourToSingleColourException() => throw new InvalidOperationException("Attempted to read single colour from multi-colour ColourInfo.");
        }

        public static implicit operator ColourInfo(Color4 colour) => (SRGBColour)colour;
        public static implicit operator Color4(ColourInfo colour) => (SRGBColour)colour;

        public static implicit operator ColourInfo(Colour4 colour) => (SRGBColour)colour;
        public static implicit operator Colour4(ColourInfo colour) => (SRGBColour)colour;
    }
}
