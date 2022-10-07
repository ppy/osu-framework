// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    /// <summary>Stores a set of four integer numbers that represent the location and size of a rectangle. The code was duplicated from <see cref="System.Drawing.Rectangle"/>.</summary>
    /// <filterpriority>1</filterpriority>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RectangleI : IEquatable<RectangleI>
    {
        /// <summary>Represents an instance of the <see cref="RectangleI"/> class with its members uninitialized.</summary>
        /// <filterpriority>1</filterpriority>
        public static RectangleI Empty { get; } = new RectangleI();

        public int X;
        public int Y;

        public int Width;
        public int Height;

        /// <summary>Initializes a new instance of the <see cref="RectangleI"/> class with the specified location and size.</summary>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle. </param>
        /// <param name="width">The width of the rectangle. </param>
        /// <param name="height">The height of the rectangle. </param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle. </param>
        public RectangleI(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Gets or sets the coordinates of the upper-left corner of this <see cref="RectangleI"/> structure.</summary>
        /// <returns>A <see cref="Vector2"/> that represents the upper-left corner of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector2I Location => new Vector2I(X, Y);

        /// <summary>Gets or sets the size of this <see cref="RectangleI"/>.</summary>
        /// <returns>A <see cref="Vector2"/> that represents the width and height of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector2I Size => new Vector2I(Width, Height);

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="RectangleI"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public int Left => X;

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="RectangleI"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public int Top => Y;

        /// <summary>Gets the x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="RectangleI"/> structure.</summary>
        /// <returns>The x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public int Right => X + Width;

        /// <summary>Gets the y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="RectangleI"/> structure.</summary>
        /// <returns>The y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="RectangleI"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public int Bottom => Y + Height;

        [Browsable(false)]
        public Vector2I TopLeft => new Vector2I(Left, Top);

        [Browsable(false)]
        public Vector2I TopRight => new Vector2I(Right, Top);

        [Browsable(false)]
        public Vector2I BottomLeft => new Vector2I(Left, Bottom);

        [Browsable(false)]
        public Vector2I BottomRight => new Vector2I(Right, Bottom);

        /// <summary>Tests whether the <see cref="Width"/> or <see cref="Height"/> property of this <see cref="RectangleI"/> has a value of zero.</summary>
        /// <returns>This property returns true if the <see cref="Width"/> or <see cref="Height"/> property of this <see cref="RectangleI"/> has a value of zero; otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public bool IsEmpty => Width <= 0 || Height <= 0;

        /// <summary>Tests whether obj is a <see cref="RectangleI"/> with the same location and size of this <see cref="RectangleI"/>.</summary>
        /// <returns>This method returns true if obj is a <see cref="RectangleI"/> and its X, Y, Width, and Height properties are equal to the corresponding properties of this <see cref="RectangleI"/>; otherwise, false.</returns>
        /// <param name="obj">The <see cref="object"/> to test. </param>
        /// <filterpriority>1</filterpriority>
        public override bool Equals(object obj) => obj is RectangleI rec && Equals(rec);

        /// <summary>Tests whether two <see cref="RectangleI"/> structures have equal location and size.</summary>
        /// <returns>This operator returns true if the two specified <see cref="RectangleI"/> structures have equal <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.</returns>
        /// <param name="right">The <see cref="RectangleI"/> structure that is to the right of the equality operator. </param>
        /// <param name="left">The <see cref="RectangleI"/> structure that is to the left of the equality operator. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator ==(RectangleI left, RectangleI right) => left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;

        /// <summary>Tests whether two <see cref="RectangleI"/> structures differ in location or size.</summary>
        /// <returns>This operator returns true if any of the <see cref="X"/> , <see cref="Y"/>, <see cref="Width"/>, or <see cref="Height"/> properties of the two <see cref="RectangleI"/> structures are unequal; otherwise false.</returns>
        /// <param name="right">The <see cref="RectangleI"/> structure that is to the right of the inequality operator. </param>
        /// <param name="left">The <see cref="RectangleI"/> structure that is to the left of the inequality operator. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator !=(RectangleI left, RectangleI right) => !(left == right);

        /// <summary>Determines if the specified point is contained within this <see cref="RectangleI"/> structure.</summary>
        /// <returns>This method returns true if the point defined by x and y is contained within this <see cref="RectangleI"/> structure; otherwise false.</returns>
        /// <param name="y">The y-coordinate of the point to test. </param>
        /// <param name="x">The x-coordinate of the point to test. </param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(float x, float y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        public bool Contains(Vector2 pt) => Contains(pt.X, pt.Y);

        public bool Contains(int x, int y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        public bool Contains(Vector2I pt) => Contains(pt.X, pt.Y);

        /// <summary>Determines if the rectangular region represented by rect is entirely contained within this <see cref="RectangleI"/> structure.</summary>
        /// <returns>This method returns true if the rectangular region represented by rect is entirely contained within the rectangular region represented by this <see cref="RectangleI"/>; otherwise false.</returns>
        /// <param name="rect">The <see cref="RectangleI"/> to test. </param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(RectangleI rect) =>
            X <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y &&
            rect.Y + rect.Height <= Y + Height;

        /// <summary>Gets the hash code for this <see cref="RectangleI"/> structure. For information about the use of hash codes, see Object.GetHashCode.</summary>
        /// <returns>The hash code for this <see cref="RectangleI"/>.</returns>
        /// <filterpriority>1</filterpriority>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => (int)(((uint)X ^ ((uint)Y << 13)) | (((uint)Y >> 0x13) ^ ((uint)Width << 0x1a)) | (((uint)Width >> 6) ^ ((uint)Height << 7)) | ((uint)Height >> 0x19));

        public int Area => Width * Height;

        public RectangleI WithPositiveExtent
        {
            get
            {
                RectangleI result = this;

                if (result.Width < 0)
                {
                    result.Width = -result.Width;
                    result.X -= result.Width;
                }

                if (Height < 0)
                {
                    result.Height = -result.Height;
                    result.Y -= result.Height;
                }

                return result;
            }
        }

        public RectangleI Inflate(int amount) => Inflate(new Vector2I(amount));

        public RectangleI Inflate(Vector2I amount) => Inflate(amount.X, amount.X, amount.Y, amount.Y);

        public RectangleI Inflate(int left, int right, int top, int bottom) => new RectangleI(
            X - left,
            Y - top,
            Width + left + right,
            Height + top + bottom);

        public RectangleI Shrink(int amount) => Shrink(new Vector2I(amount));

        public RectangleI Shrink(Vector2I amount) => Shrink(amount.X, amount.X, amount.Y, amount.Y);

        public RectangleI Shrink(int left, int right, int top, int bottom) => Inflate(-left, -right, -top, -bottom);

        /// <summary>Intersects this <see cref="RectangleI"/> structure with the specified <see cref="RectangleI"/> structure.</summary>
        /// <returns>The intersected rectangle.</returns>
        /// <param name="rect">The rectangle to intersect. </param>
        /// <filterpriority>1</filterpriority>
        public RectangleI Intersect(RectangleI rect) => Intersect(rect, this);

        /// <summary>Returns a <see cref="RectangleI"/> structure that represents the intersection of two rectangles. If there is no intersection, and empty <see cref="RectangleI"/> is returned.</summary>
        /// <returns>A third <see cref="RectangleI"/> structure the size of which represents the overlapped area of the two specified rectangles.</returns>
        /// <param name="a">A rectangle to intersect. </param>
        /// <param name="b">A rectangle to intersect. </param>
        /// <filterpriority>1</filterpriority>
        public static RectangleI Intersect(RectangleI a, RectangleI b)
        {
            int x = Math.Max(a.X, b.X);
            int num2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y = Math.Max(a.Y, b.Y);
            int num4 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            if (num2 >= x && num4 >= y)
                return new RectangleI(x, y, num2 - x, num4 - y);

            return Empty;
        }

        /// <summary>Determines if this rectangle intersects with rect.</summary>
        /// <returns>This method returns true if there is any intersection.</returns>
        /// <param name="rect">The rectangle to test. </param>
        /// <filterpriority>1</filterpriority>
        public bool IntersectsWith(RectangleI rect) =>
            rect.X <= X + Width && X <= rect.X + rect.Width && rect.Y <= Y + Height && Y <= rect.Y + rect.Height;

        /// <summary>Creates the smallest possible third rectangle that can contain both of two rectangles that form a union.</summary>
        /// <returns>A third <see cref="RectangleI"/> structure that contains both of the two rectangles that form the union.</returns>
        /// <param name="a">A rectangle to union. </param>
        /// <param name="b">A rectangle to union. </param>
        /// <filterpriority>1</filterpriority>
        public static RectangleI Union(RectangleI a, RectangleI b)
        {
            int x = Math.Min(a.X, b.X);
            int num2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y = Math.Min(a.Y, b.Y);
            int num4 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleI(x, y, num2 - x, num4 - y);
        }

        /// <summary>Adjusts the location of this rectangle by the specified amount.</summary>
        /// <returns>This method does not return a value.</returns>
        /// <param name="y">The amount to offset the location vertically. </param>
        /// <param name="x">The amount to offset the location horizontally. </param>
        /// <filterpriority>1</filterpriority>
        public RectangleI Offset(int x, int y) => new RectangleI(X + x, Y + y, Width, Height);

        public static implicit operator RectangleI(RectangleF r) => r.AABB;

        /// <summary>Converts the Location and <see cref="Size"/> of this <see cref="RectangleI"/> to a human-readable string.</summary>
        /// <returns>A string that contains the position, width, and height of this <see cref="RectangleI"/> structure¾for example, "{X=20, Y=20, Width=100, Height=50}".</returns>
        /// <filterpriority>1</filterpriority>
        public override string ToString() => $"X={X.ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Y={Y.ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Width={Width.ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Height={Height.ToString(CultureInfo.CurrentCulture)}";

        public bool Equals(RectangleI other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }
}
