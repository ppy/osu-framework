// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Computes the sorted edges of the polygon.
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

        /// <summary>
        /// Intersects 2 lines.
        /// </summary>
        /// <param name="other">Other line to intersect this with.</param>
        /// <returns>Whether the two lines intersect and, if so, the normalized distance along the line (i.e. distance
        /// divided by length of line). To compute the point of intersection, use `StartPoint + Difference * t`.</returns>
        public static (bool success, float t) Intersect(this IPolygon poly, Line line)
        {
            var result = intersect(poly.GetEdges(), line);
            return (result.idx != -1, result.t);
        }


        private static (int idx, float t) intersect(Line[] edges, Line line)
        {
            (int idx, float t) result = (-1, 0f);
            for (int i = 0; i < edges.Length; ++i)
            {
                var edgeLineIntersection = line.Intersect(edges[i]);
                if (result.idx == -1 || edgeLineIntersection.distance < result.t)
                    result = (i, edgeLineIntersection.distance);
            }

            return result;
        }


        public static Vector2[] Intersect(this IPolygon first, IPolygon second)
        {
            List<Vector2> result = new List<Vector2>();

            var edges1 = first.GetEdges();
            var edges2 = second.GetEdges();

            int i = 0;
            int j = 0;

            bool inside = second.Contains(edges1[0].StartPoint);
            if (inside)
                result.Add(edges1[0].StartPoint);
            else
            {
                for (i = 0; i < edges1.Length; ++i)
                {
                    var lineToIntersect = edges1[i];
                    var its = intersect(edges2, lineToIntersect);
                    if (its.idx != -1)
                    {
                        inside = true;
                        result.Add(lineToIntersect.At(its.t));
                        break;
                    }
                }

                if (!inside)
                {
                    if (first.Contains(edges2[0].StartPoint))
                        result.Add(edges2[0].StartPoint);
                    else
                        return Array.Empty<Vector2>();
                }
            }

            int initialI = i;
            int initialJ = -1;

            while (true)
            {
                if (inside)
                {
                    var edge = edges1[i];
                    var lineToIntersect = new Line(result.Last(), edge.EndPoint);
                    var its = intersect(edges2, lineToIntersect);
                    if (its.idx == -1)
                    {
                        result.Add(lineToIntersect.EndPoint);
                        ++i;
                        if (i == edges1.Length)
                            i = 0;

                        if (i == initialI)
                            break;
                    }
                    else
                    {
                        inside = false;
                        result.Add(lineToIntersect.At(its.t));

                        if (initialJ == -1)
                            initialJ = its.idx;
                        else if ((its.idx < j || j < initialJ) && its.idx >= initialJ)
                            break;

                        j = its.idx;
                    }
                }
                else
                {
                    var edge = edges2[j];
                    var lineToIntersect = new Line(result.Last(), edge.EndPoint);
                    var its = intersect(edges1, lineToIntersect);
                    if (its.idx == -1)
                    {
                        result.Add(lineToIntersect.EndPoint);
                        ++j;
                        if (j == edges2.Length)
                            j = 0;

                        if (j == initialJ)
                            break;
                    }
                    else
                    {
                        inside = true;
                        result.Add(lineToIntersect.At(its.t));
                        
                        if ((its.idx < i || i < initialI) && its.idx >= initialI)
                            break;
                        
                        i = its.idx;
                    }
                }
            }

            return result.ToArray();
        }
    }
}
