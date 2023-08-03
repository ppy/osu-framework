// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Holds data about each <see cref="Drawable"/>'s corner.
    /// </summary>
    public readonly struct CornersInfo : IEquatable<CornersInfo>
    {
        /// <summary>
        /// Data about four corners, stored as vector.
        /// </summary>
        public readonly Vector4 Vector;

        /// <summary>
        /// Radius of top-left corner.
        /// </summary>
        public float TopLeft => Vector.X;

        /// <summary>
        /// Radius of bottom-left corner.
        /// </summary>
        public float BottomLeft => Vector.Y;

        /// <summary>
        /// Radius of top-right corner.
        /// </summary>
        public float TopRight => Vector.Z;

        /// <summary>
        /// Radius of bottom-right corner.
        /// </summary>
        public float BottomRight => Vector.W;

        /// <summary>
        /// Initializes all four corners from <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">Vector to copy from.</param>
        public CornersInfo(Vector4 vector)
        {
            Vector = vector;
        }

        /// <summary>
        /// Initializes all four corners to the given value.
        /// </summary>
        /// <param name="value">Radius of each corner.</param>
        public CornersInfo(float value)
        {
            Vector = new Vector4(value);
        }

        /// <summary>
        /// Initializes all four corners with values for each corner.
        /// </summary>
        /// <param name="topLeft">Radius of top-left corner.</param>
        /// <param name="bottomLeft">Radius of bottom-left corner.</param>
        /// <param name="topRight">Radius of top-right corner.</param>
        /// <param name="bottomRight">Radius of bottom-right corner.</param>
        public CornersInfo(float topLeft, float bottomLeft, float topRight, float bottomRight)
        {
            Vector = new Vector4(topLeft, bottomLeft, topRight, bottomRight);
        }

        /// <summary>
        /// Gets the biggest radius among all corners.
        /// </summary>
        public float Max => Math.Max(Math.Max(Vector.X, Vector.Y), Math.Max(Vector.Z, Vector.W));

        /// <summary>
        /// Gets the least radius among all corners.
        /// </summary>
        public float Min => Math.Min(Math.Min(Vector.X, Vector.Y), Math.Min(Vector.Z, Vector.W));

        public float this[int index] => Vector[index];

        public bool Equals(CornersInfo other)
        {
            return Vector == other.Vector;
        }

        public override bool Equals(object? obj)
        {
            return obj is CornersInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Vector.GetHashCode();
        }

        public static bool operator ==(CornersInfo left, CornersInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CornersInfo left, CornersInfo right)
        {
            return !left.Equals(right);
        }

        public static CornersInfo operator +(CornersInfo left, CornersInfo right)
        {
            return new CornersInfo(left.Vector + right.Vector);
        }

        public static CornersInfo operator -(CornersInfo left, CornersInfo right)
        {
            return new CornersInfo(left.Vector - right.Vector);
        }

        public static CornersInfo operator *(CornersInfo left, float right)
        {
            return new CornersInfo(left.Vector * right);
        }

        public static CornersInfo operator /(CornersInfo left, float right)
        {
            return new CornersInfo(left.Vector / right);
        }

        public static implicit operator CornersInfo(float value) => new CornersInfo(value);

        public override string ToString() => Vector.ToString();
    }
}
