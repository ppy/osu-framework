// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.MathUtils;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    /// <summary>
    /// Represents a single line segment.
    /// </summary>
    public struct Line
    {
        /// <summary>
        /// Begin point of the line.
        /// </summary>
        public Vector2 StartPoint;

        /// <summary>
        /// End point of the line.
        /// </summary>
        public Vector2 EndPoint;

        /// <summary>
        /// The length of the line.
        /// </summary>
        public float Rho => (EndPoint - StartPoint).Length;

        /// <summary>
        /// The direction of the second point from the first.
        /// </summary>
        public float Theta => (float)Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X);

        public Vector2 Difference => EndPoint - StartPoint;

        public Vector2 Direction => Difference.Normalized();

        public Vector2 OrthogonalDirection
        {
            get
            {
                Vector2 dir = Direction;
                return new Vector2(-dir.Y, dir.X);
            }
        }

        public Line(Vector2 p1, Vector2 p2)
        {
            StartPoint = p1;
            EndPoint = p2;
        }

        /// <summary>
        /// Computes a position along the line.
        /// </summary>
        /// <param name="t">Parameterizes the position along the line to compute. t is in [0, 1], where 0 yields the
        /// start point and 1 yields the end point.</param>
        /// <returns>The position along the line.</returns>
        public Vector2 At(float t) => StartPoint + Difference * t;

        /// <summary>
        /// Intersects 2 lines.
        /// </summary>
        /// <param name="other">Other line to intersect this with.</param>
        /// <returns>Whether the two lines intersect and, if so, the normalized distance along the line (i.e. distance
        /// divided by length of line). To compute the point of intersection, use `StartPoint + Difference * t`.</returns>
        public (bool success, float distance) Intersect(Line other)
        {
            Vector2 diff1 = StartPoint - EndPoint;
            Vector2 diff2 = other.StartPoint - other.EndPoint;

            float denom = diff1.X * diff2.Y - diff1.Y * diff2.X;
            if (Precision.AlmostEquals(0, denom))
                return (false, 0); // Colinear

            Vector2 d = StartPoint - other.StartPoint;

            float t = (d.X * diff2.Y - d.Y * diff2.X) / denom;
            if (t < 0 || t > 1)
                return (false, 0);

            float u = (d.X * diff1.Y - d.Y * diff1.X) / denom;
            if (u < 0 || u > 1)
                return (false, 0);

            return (true, t);
        }

        public float Cross(Line other)
        {
            Vector2 diff1 = Difference;
            Vector2 diff2 = other.Difference;

            return diff1.X * diff2.Y - diff1.Y * diff2.X;
        }

        /// <summary>
        /// Distance squared from an arbitrary point p to this line.
        /// </summary>
        public float DistanceSquaredToPoint(Vector2 p)
        {
            return Vector2Extensions.DistanceSquared(p, ClosestPointTo(p));
        }

        /// <summary>
        /// Distance from an arbitrary point to this line.
        /// </summary>
        public float DistanceToPoint(Vector2 p)
        {
            return Vector2Extensions.Distance(p, ClosestPointTo(p));
        }

        /// <summary>
        /// Finds the point closest to the given point on this line.
        /// </summary>
        /// <remarks>
        /// See http://geometryalgorithms.com/Archive/algorithm_0102/algorithm_0102.htm, near the bottom.
        /// </remarks>
        public Vector2 ClosestPointTo(Vector2 p)
        {
            Vector2 v = EndPoint - StartPoint; // Vector from line's p1 to p2
            Vector2 w = p - StartPoint; // Vector from line's p1 to p

            // See if p is closer to p1 than to the segment
            float c1 = Vector2.Dot(w, v);
            if (c1 <= 0)
                return StartPoint;

            // See if p is closer to p2 than to the segment
            float c2 = Vector2.Dot(v, v);
            if (c2 <= c1)
                return EndPoint;

            // p is closest to point pB, between p1 and p2
            float b = c1 / c2;
            Vector2 pB = StartPoint + b * v;

            return pB;
        }

        public Matrix4 WorldMatrix()
        {
            return Matrix4.CreateRotationZ(Theta) * Matrix4.CreateTranslation(StartPoint.X, StartPoint.Y, 0);
        }

        /// <summary>
        /// It's the end of the world as we know it
        /// </summary>
        public Matrix4 EndWorldMatrix()
        {
            return Matrix4.CreateRotationZ(Theta) * Matrix4.CreateTranslation(EndPoint.X, EndPoint.Y, 0);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
