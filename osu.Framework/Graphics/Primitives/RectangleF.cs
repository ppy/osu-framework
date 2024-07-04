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
    /// <summary>Stores a set of four floating-point numbers that represent the location and size of a rectangle. The code was duplicated from <see cref="System.Drawing.RectangleF"/>.</summary>
    /// <filterpriority>1</filterpriority>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RectangleF : IEquatable<RectangleF>
    {
        /// <summary>Represents an instance of the <see cref="RectangleF"/> class with its members uninitialized.</summary>
        /// <filterpriority>1</filterpriority>
        public static RectangleF Empty { get; } = new RectangleF();

        public float X;
        public float Y;

        public float Width;
        public float Height;

        /// <summary>Initializes a new instance of the <see cref="RectangleF"/> class with the specified location and size.</summary>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle. </param>
        /// <param name="width">The width of the rectangle. </param>
        /// <param name="height">The height of the rectangle. </param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle. </param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>Initializes a new instance of the <see cref="RectangleF"/> class with the specified location and size.</summary>
        /// <param name="size">A <see cref="Vector2"/> that represents the width and height of the rectangular region. </param>
        /// <param name="location">A <see cref="Vector2"/> that represents the upper-left corner of the rectangular region. </param>
        public RectangleF(Vector2 location, Vector2 size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.X;
            Height = size.Y;
        }

        /// <summary>Gets or sets the coordinates of the upper-left corner of this <see cref="RectangleF"/> structure.</summary>
        /// <returns>A <see cref="Vector2"/> that represents the upper-left corner of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector2 Location
        {
            get => new Vector2(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>Gets or sets the size of this <see cref="RectangleF"/>.</summary>
        /// <returns>A <see cref="Vector2"/> that represents the width and height of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public Vector2 Size
        {
            get => new Vector2(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="RectangleF"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Left => X;

        /// <summary>Gets the y-coordinate of the top edge of this <see cref="RectangleF"/> structure.</summary>
        /// <returns>The y-coordinate of the top edge of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Top => Y;

        /// <summary>Gets the x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="RectangleF"/> structure.</summary>
        /// <returns>The x-coordinate that is the sum of <see cref="X"/> and <see cref="Width"/> of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Right => X + Width;

        /// <summary>Gets the y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="RectangleF"/> structure.</summary>
        /// <returns>The y-coordinate that is the sum of <see cref="Y"/> and <see cref="Height"/> of this <see cref="RectangleF"/> structure.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public float Bottom => Y + Height;

        /// <summary>Gets the top left corner of the rectangle.</summary>
        [Browsable(false)]
        public Vector2 TopLeft => new Vector2(Left, Top);

        /// <summary>Gets the top right corner of the rectangle.</summary>
        [Browsable(false)]
        public Vector2 TopRight => new Vector2(Right, Top);

        /// <summary>Gets the bottom left corner of the rectangle.</summary>
        [Browsable(false)]
        public Vector2 BottomLeft => new Vector2(Left, Bottom);

        /// <summary>Gets the bottom right corner of the rectangle.</summary>
        [Browsable(false)]
        public Vector2 BottomRight => new Vector2(Right, Bottom);

        /// <summary>Gets the center of the rectangle.</summary>
        [Browsable(false)]
        public Vector2 Centre => new Vector2(X + Width / 2, Y + Height / 2);

        /// <summary>Tests whether the <see cref="Width"/> or <see cref="Height"/> property of this <see cref="RectangleF"/> has a value of zero.</summary>
        /// <returns>This property returns true if the <see cref="Width"/> or <see cref="Height"/> property of this <see cref="RectangleF"/> has a value of zero; otherwise, false.</returns>
        /// <filterpriority>1</filterpriority>
        [Browsable(false)]
        public bool IsEmpty => Width <= 0 || Height <= 0;

        /// <summary>Tests whether obj is a <see cref="RectangleF"/> with the same location and size of this <see cref="RectangleF"/>.</summary>
        /// <returns>This method returns true if obj is a <see cref="RectangleF"/> and its X, Y, Width, and Height properties are equal to the corresponding properties of this <see cref="RectangleF"/>; otherwise, false.</returns>
        /// <param name="obj">The <see cref="object"/> to test.</param>
        /// <filterpriority>1</filterpriority>
        public override bool Equals(object obj) => obj is RectangleF rec && Equals(rec);

        /// <summary>Tests whether two <see cref="RectangleF"/> structures have equal location and size.</summary>
        /// <returns>This operator returns true if the two specified <see cref="RectangleF"/> structures have equal <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.</returns>
        /// <param name="right">The <see cref="RectangleF"/> structure that is to the right of the equality operator. </param>
        /// <param name="left">The <see cref="RectangleF"/> structure that is to the left of the equality operator. </param>
        /// <filterpriority>3</filterpriority>
        public static bool operator ==(RectangleF left, RectangleF right) => left.Equals(right);

        /// <summary>Tests whether two <see cref="RectangleF"/> structures differ in location or size.</summary>
        /// <returns>This operator returns true if any of the <see cref="X"/> , <see cref="Y"/>, <see cref="Width"/>, or <see cref="Height"/> properties of the two <see cref="RectangleF"/> structures are unequal; otherwise false.</returns>
        /// <param name="right">The <see cref="RectangleF"/> structure that is to the right of the inequality operator.</param>
        /// <param name="left">The <see cref="RectangleF"/> structure that is to the left of the inequality operator.</param>
        /// <filterpriority>3</filterpriority>
        public static bool operator !=(RectangleF left, RectangleF right) => !(left == right);

        public static RectangleF operator *(RectangleF rectangle, float scale) => new RectangleF(rectangle.X * scale, rectangle.Y * scale, rectangle.Width * scale, rectangle.Height * scale);

        public static RectangleF operator /(RectangleF rectangle, float scale) => new RectangleF(rectangle.X / scale, rectangle.Y / scale, rectangle.Width / scale, rectangle.Height / scale);

        public static RectangleF operator *(RectangleF rectangle, Vector2 scale) => new RectangleF(rectangle.X * scale.X, rectangle.Y * scale.Y, rectangle.Width * scale.X, rectangle.Height * scale.Y);

        public static RectangleF operator /(RectangleF rectangle, Vector2 scale) => new RectangleF(rectangle.X / scale.X, rectangle.Y / scale.Y, rectangle.Width / scale.X, rectangle.Height / scale.Y);

        /// <summary>Determines if the specified point is contained within this <see cref="RectangleF"/> structure.</summary>
        /// <returns>This method returns true if the point defined by x and y is contained within this <see cref="RectangleF"/> structure; otherwise false.</returns>
        /// <param name="y">The y-coordinate of the point to test.</param>
        /// <param name="x">The x-coordinate of the point to test.</param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(float x, float y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        /// <summary>Determines if the specified point is contained within this <see cref="RectangleF"/> structure.</summary>
        /// <returns>This method returns true if the point defined by x and y is contained within this <see cref="RectangleF"/> structure; otherwise false.</returns>
        /// <param name="pt">The point to test against this <see cref="RectangleF"/>.</param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(Vector2 pt) => Contains(pt.X, pt.Y);

        /// <summary>Determines if the specified point is contained within this <see cref="RectangleF"/> structure.</summary>
        /// <returns>This method returns true if the point represented by the pt parameter is contained within this <see cref="RectangleF"/> structure; otherwise false.</returns>
        /// <param name="pt">The <see cref="Vector2I"/> to test.</param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(Vector2I pt) => Contains(pt.X, pt.Y);

        /// <summary>Determines if the rectangular region represented by rect is entirely contained within this <see cref="RectangleF"/> structure.</summary>
        /// <returns>This method returns true if the rectangular region represented by rect is entirely contained within the rectangular region represented by this <see cref="RectangleF"/>; otherwise false.</returns>
        /// <param name="rect">The <see cref="RectangleF"/> to test.</param>
        /// <filterpriority>1</filterpriority>
        public bool Contains(RectangleF rect) =>
            X <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y &&
            rect.Y + rect.Height <= Y + Height;

        /// <summary>Gets the hash code for this <see cref="RectangleF"/> structure. For information about the use of hash codes, see Object.GetHashCode.</summary>
        /// <returns>The hash code for this <see cref="RectangleF"/>.</returns>
        /// <filterpriority>1</filterpriority>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() =>
            (int)(((uint)X ^ ((uint)Y << 13)) | (((uint)Y >> 0x13) ^ ((uint)Width << 0x1a)) | (((uint)Width >> 6) ^ ((uint)Height << 7)) | ((uint)Height >> 0x19));

        /// <summary>Gets the Area of this <see cref="RectangleF"/>.</summary>
        public float Area => Width * Height;

        /// <summary>
        /// Gets this <see cref="RectangleF"/> with positive width and height.
        /// This is useful if you have a <see cref="RectangleF"/> with negative <see cref="Width"/> or <see cref="Height"/>.
        /// </summary>
        /// <example>
        /// var rect = new <see cref="RectangleF"/> { <see cref="Width"/> = -200, <see cref="Height"/> = -300 }
        ///
        /// rect.<see cref="WithPositiveExtent"/> will result in
        /// Width = 200
        /// Height = 300
        /// X = -200
        /// Y = -300
        /// </example>
        public RectangleF WithPositiveExtent
        {
            get
            {
                RectangleF result = this;

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

        /// <summary>
        /// Gets a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> increased.
        /// The center of that <see cref="RectangleF"/> will stay at that location.
        /// </summary>
        /// <param name="amount">The amount both <see cref="Width"/> and <see cref="Height"/> will be increased in the returned <see cref="RectangleF"/>.</param>
        /// <returns>This method returns a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> inflated by the given <paramref name="amount"/>.</returns>
        public RectangleF Inflate(float amount) => Inflate(new Vector2(amount, amount));

        /// <summary>
        /// Gets a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> increased.
        /// The center of that <see cref="RectangleF"/> will stay at that location.
        /// </summary>
        /// <param name="amount">The amount both <see cref="Width"/> and <see cref="Height"/> will be increased in the returned <see cref="RectangleF"/>.</param>
        /// <returns>This method returns a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> inflated by the given <paramref name="amount"/>.</returns>
        public RectangleF Inflate(Vector2 amount) => Inflate(new MarginPadding { Left = amount.X, Right = amount.X, Top = amount.Y, Bottom = amount.Y });

        /// <summary>
        /// Gets a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> increased.
        /// The center of that <see cref="RectangleF"/> will stay at that location.
        /// </summary>
        /// <param name="amount">The amount both <see cref="Width"/> and <see cref="Height"/> will be increased in the returned <see cref="RectangleF"/>.</param>
        /// <returns>This method returns a <see cref="RectangleF"/> with both <see cref="Width"/> and <see cref="Height"/> inflated by the given <paramref name="amount"/>.</returns>
        public RectangleF Inflate(MarginPadding amount) => new RectangleF(
            X - amount.Left,
            Y - amount.Top,
            Width + amount.TotalHorizontal,
            Height + amount.TotalVertical);

        public RectangleF Shrink(float amount) => Shrink(new Vector2(amount, amount));

        public RectangleF Shrink(Vector2 amount) => Shrink(new MarginPadding { Left = amount.X, Right = amount.X, Top = amount.Y, Bottom = amount.Y });

        public RectangleF Shrink(MarginPadding amount) => Inflate(-amount);

        /// <summary>Replaces this <see cref="RectangleF"/> structure with the intersection of itself and the specified <see cref="RectangleF"/> structure.</summary>
        /// <returns>This method does not return a value.</returns>
        /// <param name="rect">The rectangle to intersect.</param>
        /// <filterpriority>1</filterpriority>
        public void Intersect(RectangleF rect)
        {
            RectangleF ef = Intersect(rect, this);
            X = ef.X;
            Y = ef.Y;
            Width = ef.Width;
            Height = ef.Height;
        }

        /// <summary>Returns a <see cref="RectangleF"/> structure that represents the intersection of two rectangles. If there is no intersection, and empty <see cref="RectangleF"/> is returned.</summary>
        /// <returns>A third <see cref="RectangleF"/> structure the size of which represents the overlapped area of the two specified rectangles.</returns>
        /// <param name="a">A rectangle to intersect.</param>
        /// <param name="b">A rectangle to intersect.</param>
        /// <filterpriority>1</filterpriority>
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x = Math.Max(a.X, b.X);
            float num2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y = Math.Max(a.Y, b.Y);
            float num4 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            if (num2 >= x && num4 >= y)
                return new RectangleF(x, y, num2 - x, num4 - y);

            return Empty;
        }

        /// <summary>Determines if this rectangle intersects with rect.</summary>
        /// <returns>This method returns true if there is any intersection.</returns>
        /// <param name="rect">The rectangle to test.</param>
        /// <filterpriority>1</filterpriority>
        public bool IntersectsWith(RectangleF rect) =>
            rect.X <= X + Width && X <= rect.X + rect.Width && rect.Y <= Y + Height && Y <= rect.Y + rect.Height;

        /// <summary>Determines if this rectangle intersects with rect.</summary>
        /// <returns>This method returns true if there is any intersection.</returns>
        /// <param name="rect">The rectangle to test.</param>
        /// <filterpriority>1</filterpriority>
        public bool IntersectsWith(RectangleI rect) =>
            rect.X <= X + Width && X <= rect.X + rect.Width && rect.Y <= Y + Height && Y <= rect.Y + rect.Height;

        /// <summary>Creates the smallest possible third rectangle that can contain both of two rectangles that form a union.</summary>
        /// <returns>A third <see cref="RectangleF"/> structure that contains both of the two rectangles that form the union.</returns>
        /// <param name="a">The first rectangle to union.</param>
        /// <param name="b">The second rectangle to union.</param>
        /// <filterpriority>1</filterpriority>
        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            float x = Math.Min(a.X, b.X);
            float num2 = Math.Max(a.X + a.Width, b.X + b.Width);
            float y = Math.Min(a.Y, b.Y);
            float num4 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            return new RectangleF(x, y, num2 - x, num4 - y);
        }

        /// <summary>Adjusts the location of this rectangle by the specified amount.</summary>
        /// <returns>This method does not return a value.</returns>
        /// <param name="pos">The amount to offset the location.</param>
        /// <filterpriority>1</filterpriority>
        public RectangleF Offset(Vector2 pos) => Offset(pos.X, pos.Y);

        /// <summary>Adjusts the location of this rectangle by the specified amount.</summary>
        /// <returns>This method does not return a value.</returns>
        /// <param name="y">The amount to offset the location vertically.</param>
        /// <param name="x">The amount to offset the location horizontally.</param>
        /// <filterpriority>1</filterpriority>
        public RectangleF Offset(float x, float y) => new RectangleF(X + x, Y + y, Width, Height);

        internal float DistanceSquared(Vector2 localSpacePos)
        {
            Vector2 dist = new Vector2(
                Math.Max(0.0f, Math.Max(localSpacePos.X - Right, Left - localSpacePos.X)),
                Math.Max(0.0f, Math.Max(localSpacePos.Y - Bottom, Top - localSpacePos.Y))
            );

            return dist.LengthSquared;
        }

        internal float DistanceExponentiated(Vector2 localSpacePos, float exponent)
        {
            float distX = Math.Max(0.0f, Math.Max(localSpacePos.X - Right, Left - localSpacePos.X));
            float distY = Math.Max(0.0f, Math.Max(localSpacePos.Y - Bottom, Top - localSpacePos.Y));

            return MathF.Pow(distX, exponent) + MathF.Pow(distY, exponent);
        }

        // This could be optimized further in the future, but made for a simple implementation right now.
        public RectangleI AABB => ((Quad)this).AABB;

        /// <summary>
        /// Constructs a <see cref="RectangleF"/> from left, top, right, and bottom coordinates.
        /// </summary>
        /// <param name="left">The left coordinate.</param>
        /// <param name="top">The top coordinate.</param>
        /// <param name="right">The right coordinate.</param>
        /// <param name="bottom">The bottom coordinate.</param>
        /// <returns>The <see cref="RectangleF"/>.</returns>
        public static RectangleF FromLTRB(float left, float top, float right, float bottom) => new RectangleF(left, top, right - left, bottom - top);

        /// <summary>
        /// Creates a new <see cref="RectangleF"/> in relative coordinate space to another <see cref="RectangleF"/>.
        /// </summary>
        /// <param name="other">The other <see cref="RectangleF"/>.</param>
        /// <returns>The relative coordinate space representation of this <see cref="RectangleF"/> in <paramref name="other"/>.</returns>
        public RectangleF RelativeIn(RectangleF other)
        {
            float scaleX = Width / other.Width;
            float scaleY = Height / other.Height;
            return new RectangleF((X - other.X) / other.Width, (Y - other.Y) / other.Height, scaleX, scaleY);
        }

        /// <summary>
        /// Create a new <see cref="RectangleF"/> congruent to this rectangle but with non-negative <see cref="Width"/> and <see cref="Height"/>.
        /// </summary>
        public RectangleF Normalize() => new RectangleF(Math.Min(Left, Right), Math.Min(Top, Bottom), Math.Abs(Width), Math.Abs(Height));

        /// <summary>Converts the specified <see cref="RectangleI"/> structure to a <see cref="RectangleF"/> structure.</summary>
        /// <returns>The <see cref="RectangleF"/> structure that is converted from the specified <see cref="RectangleI"/> structure.</returns>
        /// <param name="r">The <see cref="RectangleI"/> structure to convert.</param>
        /// <filterpriority>3</filterpriority>
        public static implicit operator RectangleF(RectangleI r) => new RectangleF(r.X, r.Y, r.Width, r.Height);

        public static implicit operator System.Drawing.RectangleF(RectangleF r) => new System.Drawing.RectangleF(r.X, r.Y, r.Width, r.Height);

        /// <summary>Converts the Location and <see cref="Size"/> of this <see cref="RectangleF"/> to a human-readable string.</summary>
        /// <returns>A string that contains the position, width, and height of this <see cref="RectangleF"/> structure¾for example, "{X=20, Y=20, Width=100, Height=50}".</returns>
        /// <filterpriority>1</filterpriority>
        public override string ToString() => $"X={Math.Round(X, 3).ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Y={Math.Round(Y, 3).ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Width={Math.Round(Width, 3).ToString(CultureInfo.CurrentCulture)}, "
                                             + $"Height={Math.Round(Height, 3).ToString(CultureInfo.CurrentCulture)}";

        public bool Equals(RectangleF other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }
}
