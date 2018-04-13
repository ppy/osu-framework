// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Extensions.PolygonExtensions;
using OpenTK;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Primitives
{
    public struct Quad : IConvexPolygon, IEquatable<Quad>
    {
        public Vector2 TopLeft;
        public Vector2 TopRight;
        public Vector2 BottomLeft;
        public Vector2 BottomRight;

        public Quad(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public Quad(float x, float y, float width, float height)
            : this()
        {
            TopLeft = new Vector2(x, y);
            TopRight = new Vector2(x + width, y);
            BottomLeft = new Vector2(x, y + height);
            BottomRight = new Vector2(x + width, y + height);
        }

        public static implicit operator Quad(RectangleI r) => FromRectangle(r);
        public static implicit operator Quad(RectangleF r) => FromRectangle(r);

        public static Quad FromRectangle(RectangleF rectangle)
        {
            return new Quad(new Vector2(rectangle.Left, rectangle.Top),
                new Vector2(rectangle.Right, rectangle.Top),
                new Vector2(rectangle.Left, rectangle.Bottom),
                new Vector2(rectangle.Right, rectangle.Bottom));
        }

        public static Quad operator *(Quad r, Matrix3 m)
        {
            return new Quad(
                Vector2Extensions.Transform(r.TopLeft, m),
                Vector2Extensions.Transform(r.TopRight, m),
                Vector2Extensions.Transform(r.BottomLeft, m),
                Vector2Extensions.Transform(r.BottomRight, m));
        }

        public Matrix2 BasisTransform
        {
            get
            {
                Vector2 row0 = TopRight - TopLeft;
                Vector2 row1 = BottomLeft - TopLeft;

                if (row0 != Vector2.Zero)
                    row0 /= row0.LengthSquared;

                if (row1 != Vector2.Zero)
                    row1 /= row1.LengthSquared;

                return new Matrix2(
                    row0.X, row0.Y,
                    row1.X, row1.Y);
            }
        }

        public Vector2 Centre => (TopLeft + TopRight + BottomLeft + BottomRight) / 4;
        public Vector2 Size => new Vector2(Width, Height);

        public float Width => Vector2Extensions.Distance(TopLeft, TopRight);
        public float Height => Vector2Extensions.Distance(TopLeft, BottomLeft);

        public RectangleI AABB
        {
            get
            {
                int xMin = (int)Math.Floor(Math.Min(TopLeft.X, Math.Min(TopRight.X, Math.Min(BottomLeft.X, BottomRight.X))));
                int yMin = (int)Math.Floor(Math.Min(TopLeft.Y, Math.Min(TopRight.Y, Math.Min(BottomLeft.Y, BottomRight.Y))));
                int xMax = (int)Math.Ceiling(Math.Max(TopLeft.X, Math.Max(TopRight.X, Math.Max(BottomLeft.X, BottomRight.X))));
                int yMax = (int)Math.Ceiling(Math.Max(TopLeft.Y, Math.Max(TopRight.Y, Math.Max(BottomLeft.Y, BottomRight.Y))));

                return new RectangleI(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        public RectangleF AABBFloat
        {
            get
            {
                float xMin = Math.Min(TopLeft.X, Math.Min(TopRight.X, Math.Min(BottomLeft.X, BottomRight.X)));
                float yMin = Math.Min(TopLeft.Y, Math.Min(TopRight.Y, Math.Min(BottomLeft.Y, BottomRight.Y)));
                float xMax = Math.Max(TopLeft.X, Math.Max(TopRight.X, Math.Max(BottomLeft.X, BottomRight.X)));
                float yMax = Math.Max(TopLeft.Y, Math.Max(TopRight.Y, Math.Max(BottomLeft.Y, BottomRight.Y)));

                return new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        public Vector2[] Vertices => new[] { TopLeft, TopRight, BottomRight, BottomLeft };
        public Vector2[] AxisVertices => Vertices;

        public bool Contains(Vector2 pos)
        {
            return
                new Triangle(BottomRight, BottomLeft, TopRight).Contains(pos) ||
                new Triangle(TopLeft, TopRight, BottomLeft).Contains(pos);
        }

        public float Area => new Triangle(BottomRight, BottomLeft, TopRight).Area + new Triangle(TopLeft, TopRight, BottomLeft).Area;

        public float ConservativeArea
        {
            get
            {
                if (Precision.AlmostEquals(TopLeft.Y, TopRight.Y))
                    return Math.Abs((TopLeft.Y - BottomLeft.Y) * (TopLeft.X - TopRight.X));

                // Uncomment this to speed this computation up at the cost of losing accuracy when considering shearing.
                //return Math.Sqrt(Vector2Extensions.DistanceSquared(TopLeft, TopRight) * Vector2Extensions.DistanceSquared(TopLeft, BottomLeft));

                Vector2 d1 = TopLeft - TopRight;
                float lsq1 = d1.LengthSquared;

                Vector2 d2 = TopLeft - BottomLeft;
                float lsq2 = Vector2Extensions.DistanceSquared(d2, d1 * Vector2.Dot(d2, d1 * MathHelper.InverseSqrtFast(lsq1)));

                return (float)Math.Sqrt(lsq1 * lsq2);
            }
        }

        public bool Intersects(IConvexPolygon other)
        {
            return (this as IConvexPolygon).Intersects(other);
        }

        public bool Equals(Quad other)
        {
            return
                TopLeft == other.TopLeft &&
                TopRight == other.TopRight &&
                BottomLeft == other.BottomLeft &&
                BottomRight == other.BottomRight;
        }

        public bool AlmostEquals(Quad other)
        {
            return
                Precision.AlmostEquals(TopLeft.X, other.TopLeft.X) &&
                Precision.AlmostEquals(TopLeft.Y, other.TopLeft.Y) &&
                Precision.AlmostEquals(TopRight.X, other.TopRight.X) &&
                Precision.AlmostEquals(TopRight.Y, other.TopRight.Y) &&
                Precision.AlmostEquals(BottomLeft.X, other.BottomLeft.X) &&
                Precision.AlmostEquals(BottomLeft.Y, other.BottomLeft.Y) &&
                Precision.AlmostEquals(BottomRight.X, other.BottomRight.X) &&
                Precision.AlmostEquals(BottomRight.Y, other.BottomRight.Y);
        }

        public override string ToString() => $"{TopLeft} {TopRight} {BottomLeft} {BottomRight}";
    }
}
