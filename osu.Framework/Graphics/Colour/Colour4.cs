// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Colour
{
    public readonly struct Colour4 : IEquatable<Colour4>
    {
        /// <summary>
        /// Represents the red component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float R;

        /// <summary>
        /// Represents the green component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float G;

        /// <summary>
        /// Represents the blue component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float B;

        /// <summary>
        /// Represents the alpha component of the RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float A;

        #region Constructors

        public Colour4(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Colour4(byte r, byte g, byte b, byte a)
        {
            R = r / (float)byte.MaxValue;
            G = g / (float)byte.MaxValue;
            B = b / (float)byte.MaxValue;
            A = a / (float)byte.MaxValue;
        }

        #endregion

        #region Operator Overloads

        public static Colour4 operator *(Colour4 first, Colour4 second) =>
            new Colour4(first.R * second.R, first.G * second.G, first.B * second.B, first.A * second.A);

        public static Colour4 operator +(Colour4 first, Colour4 second) =>
            new Colour4(first.R + second.R, first.G + second.G, first.B + second.B, first.A + second.A);

        public static Colour4 operator *(Colour4 first, float second) =>
            new Colour4(first.R * second, first.G * second, first.B * second, first.A * second);

        public static Colour4 operator /(Colour4 first, float second) => first * (1 / second);

        #endregion

        #region Equality

        public bool Equals(Colour4 other) =>
            R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);

        public override bool Equals(object obj) =>
            obj is Colour4 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        #endregion

        public override string ToString() => $"(R, G, B, A) = ({R:F}, {G:F}, {B:F}, {A:F})";
    }
}
