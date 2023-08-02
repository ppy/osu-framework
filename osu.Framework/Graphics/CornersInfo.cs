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
        public readonly Vector4 Vector;

        public float TopLeft => Vector.X;
        public float BottomLeft => Vector.Y;
        public float TopRight => Vector.Z;
        public float BottomRight => Vector.W;

        public CornersInfo(Vector4 vector)
        {
            Vector = vector;
        }

        public CornersInfo(float value)
        {
            Vector = new Vector4(value);
        }

        public CornersInfo(float topLeft, float bottomLeft, float topRight, float bottomRight)
        {
            Vector = new Vector4(topLeft, bottomLeft, topRight, bottomRight);
        }

        public float Max()
        {
            return Math.Max(Math.Max(Vector.X, Vector.Y), Math.Max(Vector.Z, Vector.W));
        }

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
    }
}
