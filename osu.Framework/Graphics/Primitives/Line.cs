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
    public readonly struct Line
    {
        /// <summary>
        /// Begin point of the line.
        /// </summary>
        public readonly Vector2 StartPoint;

        /// <summary>
        /// End point of the line.
        /// </summary>
        public readonly Vector2 EndPoint;

        /// <summary>
        /// The length of the line.
        /// </summary>
        public float Rho => (EndPoint - StartPoint).Length;

        /// <summary>
        /// The direction of the second point from the first.
        /// </summary>
        public float Theta => (float)Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X);

        /// <summary>
        /// The direction of this <see cref="Line"/>.
        /// </summary>
        public Vector2 Direction => EndPoint - StartPoint;

        /// <summary>
        /// The normalized direction of this <see cref="Line"/>.
        /// </summary>
        public Vector2 DirectionNormalized => Direction.Normalized();

        public Vector2 OrthogonalDirection
        {
            get
            {
                Vector2 dir = DirectionNormalized;
                return new Vector2(-dir.Y, dir.X);
            }
        }

        public Line(Vector2 p1, Vector2 p2)
        {
            StartPoint = p1;
            EndPoint = p2;
        }

        /// <summary>
        /// Computes a position along this line.
        /// </summary>
        /// <param name="t">A parameter representing the position along the line to compute. 0 yields the start point and 1 yields the end point.</param>
        /// <returns>The position along the line.</returns>
        public Vector2 At(float t) => StartPoint + Direction * t;

        /// <summary>
        /// Intersects this line with another.
        /// </summary>
        /// <param name="other">The line to intersect with.</param>
        /// <returns>Whether the two lines intersect and, if so, the distance along this line at which the intersection occurs.
        /// An intersection may occur even if the two lines don't touch, at which point the parameter will be outside the [0, 1] range.
        /// To compute the point of intersection, <see cref="At"/>.</returns>
        public (bool success, float distance) IntersectWith(Line other)
        {
            Vector2 diff1 = Direction;
            Vector2 diff2 = other.Direction;

            float denom = diff1.X * diff2.Y - diff1.Y * diff2.X;

            if (Precision.AlmostEquals(0, denom))
                return (false, 0); // Co-linear

            Vector2 d = other.StartPoint - StartPoint;
            float t = (d.X * diff2.Y - d.Y * diff2.X) / denom;

            return (true, t);
        }

        /// <summary>
        /// Distance squared from an arbitrary point p to this line.
        /// </summary>
        public float DistanceSquaredToPoint(Vector2 p) => Vector2Extensions.DistanceSquared(p, ClosestPointTo(p));

        /// <summary>
        /// Distance from an arbitrary point to this line.
        /// </summary>
        public float DistanceToPoint(Vector2 p) => Vector2Extensions.Distance(p, ClosestPointTo(p));

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

        public Matrix4 WorldMatrix() => Matrix4.CreateRotationZ(Theta) * Matrix4.CreateTranslation(StartPoint.X, StartPoint.Y, 0);

        /// <summary>
        /// It's the end of the world as we know it
        /// </summary>
        public Matrix4 EndWorldMatrix() => Matrix4.CreateRotationZ(Theta) * Matrix4.CreateTranslation(EndPoint.X, EndPoint.Y, 0);

        public override string ToString() => $"{StartPoint} -> {EndPoint}";
    }
}
