// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Extensions.PolygonExtensions
{
    /// <summary>
    /// Todo: Support segment containment and circles.
    /// Todo: Might be overkill, but possibly support convex decomposition?
    /// </summary>
    public static class ConvexPolygonExtensions
    {
        /// <summary>
        /// Determines whether two convex polygons intersect.
        /// </summary>
        /// <param name="first">The first polygon.</param>
        /// <param name="second">The second polygon.</param>
        /// <returns>Whether the two polygons intersect.</returns>
        public static bool Intersects<TPolygon1, TPolygon2>(this TPolygon1 first, TPolygon2 second)
            where TPolygon1 : IConvexPolygon
            where TPolygon2 : IConvexPolygon
        {
            var firstVertices = first.GetVertices();
            var secondVertices = second.GetVertices();

            Span<Vector2> axisBuffer = stackalloc Vector2[Math.Max(firstVertices.Length, secondVertices.Length)];

            bool result = intersects(first.GetAxes(axisBuffer), firstVertices, secondVertices);
            result = result && intersects(second.GetAxes(axisBuffer), firstVertices, secondVertices);

            return result;
        }

        /// <summary>
        /// Determines whether two sets of vertices intersect along a set of axes.
        /// </summary>
        /// <param name="axes">The axes to check for intersections along.</param>
        /// <param name="firstVertices">The first set of vertices.</param>
        /// <param name="secondVertices">The second set of vertices.</param>
        /// <returns>Whether there is an intersection between <paramref name="firstVertices"/> and <paramref name="secondVertices"/> along any of <paramref name="axes"/>.</returns>
        private static bool intersects(ReadOnlySpan<Vector2> axes, ReadOnlySpan<Vector2> firstVertices, ReadOnlySpan<Vector2> secondVertices)
        {
            foreach (Vector2 axis in axes)
            {
                ProjectionRange firstRange = new ProjectionRange(axis, firstVertices);
                ProjectionRange secondRange = new ProjectionRange(axis, secondVertices);

                if (!firstRange.Overlaps(secondRange))
                    return false;
            }

            return true;
        }
    }
}
