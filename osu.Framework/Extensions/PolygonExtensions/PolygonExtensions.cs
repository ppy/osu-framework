// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK;
using System;

namespace osu.Framework.Extensions.PolygonExtensions
{
    public static class PolygonExtensions
    {
        /// <summary>
        /// Computes the axes for each edge in a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to return the axes of.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes of the polygon.</returns>
        public static Vector2[] GetAxes(this IPolygon polygon, bool normalize = false)
        {
            Vector2[] axes = new Vector2[polygon.AxisVertices.Length];

            for (int i = 0; i < polygon.AxisVertices.Length; i++)
            {
                // Construct an edge between two sequential points
                Vector2 v1 = polygon.AxisVertices[i];
                Vector2 v2 = polygon.AxisVertices[i == polygon.AxisVertices.Length - 1 ? 0 : i + 1];
                Vector2 edge = v2 - v1;

                // Find the normal to the edge
                Vector2 normal = new Vector2(-edge.Y, edge.X);

                if (normalize)
                    normal = Vector2.Normalize(normal);

                axes[i] = normal;
            }

            return axes;
        }

        public static Vector2[] GetVerticesClockwise(this IPolygon polygon)
        {
            Vector2[] vertices = polygon.Vertices;

            float rotation = GetRotation(vertices);

            if (rotation < 0)
                Array.Reverse(vertices);

            return vertices;
        }

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
        /// Computes the edges of the polygon in clockwise-sorted order.
        /// </summary>
        /// <param name="polygon">The polygon to compute the edges for.</param>
        /// <returns>A list of line segments, each corresponding to one edge of the polygon.</returns>
        public static Line[] GetEdges(this IPolygon polygon)
        {
            Vector2[] vertices = polygon.Vertices;
            Line[] lines = new Line[vertices.Length];

            float rotation = 0;
            for (int i = 0; i < lines.Length - 1; ++i)
            {
                lines[i] = new Line(vertices[i], vertices[i + 1]);
                rotation += (lines[i].EndPoint.X - lines[i].StartPoint.X) * (lines[i].EndPoint.Y + lines[i].StartPoint.Y);
            }

            lines[lines.Length - 1] = new Line(vertices[lines.Length - 1], vertices[0]);
            rotation += (lines[lines.Length - 1].EndPoint.X - lines[lines.Length - 1].StartPoint.X) * (lines[lines.Length - 1].EndPoint.Y + lines[lines.Length - 1].StartPoint.Y);

            if (rotation < 0)
            {
                for (int i = 0; i < lines.Length; ++i)
                    lines[i] = new Line(lines[i].EndPoint, lines[i].StartPoint);
                Array.Reverse(lines);
            }

            return lines;
        }
    }
}
