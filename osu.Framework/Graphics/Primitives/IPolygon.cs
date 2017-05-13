﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Primitives
{
    public interface IPolygon
    {
        /// <summary>
        /// The vertices for this polygon.
        /// </summary>
        Vector2[] Vertices { get; }

        /// <summary>
        /// The vertices for this polygon that are used to compute the axes of the polygon.
        /// <para>
        /// Optimisation: Edges that would form duplicate normals as other edges
        /// in the polygon do not need their vertices added to this array.
        /// </para>
        /// </summary>
        Vector2[] AxisVertices { get; }
    }

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
    }
}
