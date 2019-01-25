// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

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
}
