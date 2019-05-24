// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Graphics
{
    public static class Vector2Extensions
    {
        /// <summary>Transform a Position by the given Matrix</summary>
        /// <param name="pos">The position to transform</param>
        /// <param name="mat">The desired transformation</param>
        /// <returns>The transformed position</returns>
        public static Vector2 Transform(Vector2 pos, Matrix3 mat)
        {
            Transform(ref pos, ref mat, out Vector2 result);
            return result;
        }

        /// <summary>Transform a Position by the given Matrix</summary>
        /// <param name="pos">The position to transform</param>
        /// <param name="mat">The desired transformation</param>
        /// <param name="result">The transformed vector</param>
        public static void Transform(ref Vector2 pos, ref Matrix3 mat, out Vector2 result)
        {
            result.X = mat.Row0.X * pos.X + mat.Row1.X * pos.Y + mat.Row2.X;
            result.Y = mat.Row0.Y * pos.X + mat.Row1.Y * pos.Y + mat.Row2.Y;
        }

        /// <summary>
        /// Compute the euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <returns>The distance</returns>
        public static float Distance(Vector2 vec1, Vector2 vec2)
        {
            Distance(ref vec1, ref vec2, out float result);
            return result;
        }

        /// <summary>
        /// Compute the euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="result">The distance</param>
        public static void Distance(ref Vector2 vec1, ref Vector2 vec2, out float result)
        {
            result = (float)Math.Sqrt((vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y));
        }

        /// <summary>
        /// Compute the squared euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <returns>The squared distance</returns>
        public static float DistanceSquared(Vector2 vec1, Vector2 vec2)
        {
            DistanceSquared(ref vec1, ref vec2, out float result);
            return result;
        }

        /// <summary>
        /// Compute the squared euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="result">The squared distance</param>
        public static void DistanceSquared(ref Vector2 vec1, ref Vector2 vec2, out float result)
        {
            result = (vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y);
        }

        /// <summary>
        /// Retrieves the rotation of a set of vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>Twice the area enclosed by the vertices.
        /// The vertices are in clockwise order if the value is positive.
        /// The vertices are in counter-clockwise order if the value is negative.</returns>
        public static float GetRotation(ReadOnlySpan<Vector2> vertices)
        {
            float rotation = 0;
            for (int i = 0; i < vertices.Length - 1; ++i)
            {
                var vi = vertices[i];
                var vj = vertices[i + 1];

                rotation += (vj.X - vi.X) * (vj.Y + vi.Y);
            }

            rotation += (vertices[0].X - vertices[vertices.Length - 1].X) * (vertices[0].Y + vertices[vertices.Length - 1].Y);

            return rotation;
        }

        /// <summary>
        /// Sorts a set of vertices in clockwise order.
        /// </summary>
        /// <param name="vertices">The vertices to sort.</param>
        public static void ClockwiseSort(Span<Vector2> vertices)
        {
            if (GetRotation(vertices) < 0)
                vertices.Reverse();
        }

        /// <summary>
        /// Determines whether a point is within the right half-plane of a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="point">The point.</param>
        /// <returns>Whether <paramref name="point"/> is in the right half-plane of <paramref name="line"/>.
        /// If the point is colinear to the line, it is said to be in the right half-plane of the line.
        /// </returns>
        public static bool InRightHalfPlaneOf(this Vector2 point, Line line)
        {
            var diff1 = line.Direction;
            var diff2 = point - line.StartPoint;

            return diff1.X * diff2.Y - diff1.Y * diff2.X <= 0;
        }
    }
}
