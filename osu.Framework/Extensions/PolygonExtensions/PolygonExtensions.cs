// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Extensions.PolygonExtensions
{
    public static class PolygonExtensions
    {
        /// <summary>
        /// Computes the axes for each edge in a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to compute the axes of.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes of the polygon.</returns>
        public static Span<Vector2> GetAxes<TPolygon>(TPolygon polygon, bool normalize = false)
            where TPolygon : IPolygon
        {
            var axisVertices = polygon.GetAxisVertices();
            return getAxes(axisVertices, new Vector2[axisVertices.Length], normalize);
        }

        /// <summary>
        /// Computes the axes for each edge in a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to compute the axes of.</param>
        /// <param name="buffer">A buffer to be used as storage for the axes. Must have a length of at least the count of vertices in <paramref name="polygon"/>.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes of the polygon. Returned as a slice of <paramref name="buffer"/>.</returns>
        public static Span<Vector2> GetAxes<TPolygon>(this TPolygon polygon, Span<Vector2> buffer, bool normalize = false)
            where TPolygon : IPolygon
            => getAxes(polygon.GetAxisVertices(), buffer, normalize);

        /// <summary>
        /// Computes the axes for a set of vertices.
        /// </summary>
        /// <param name="vertices">The vertices to compute the axes for.</param>
        /// <param name="buffer">A buffer to be used as storage for the axes. Must have a length of at least the count of <paramref name="vertices"/>.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes represented by <paramref name="vertices"/>. Returned as a slice of <paramref name="buffer"/>.</returns>
        private static Span<Vector2> getAxes(ReadOnlySpan<Vector2> vertices, Span<Vector2> buffer, bool normalize = false)
        {
            if (buffer.Length < vertices.Length)
                throw new ArgumentException($"Axis buffer must have a length of {vertices.Length}, but was {buffer.Length}.", nameof(buffer));

            for (int i = 0; i < vertices.Length; i++)
            {
                // Construct an edge between two sequential points
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[i == vertices.Length - 1 ? 0 : i + 1];
                Vector2 edge = v2 - v1;

                // Find the normal to the edge
                Vector2 normal = new Vector2(-edge.Y, edge.X);

                if (normalize)
                    normal = Vector2.Normalize(normal);

                buffer[i] = normal;
            }

            return buffer.Slice(0, vertices.Length);
        }
    }
}
