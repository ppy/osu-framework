// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Primitives
{
    public interface IPolygon
    {
        /// <summary>
        /// The number of vertices in this polygon.
        /// </summary>
        int VertexCount { get; }

        /// <summary>
        /// The number of axes formed by the vertices of this polygon.
        /// </summary>
        int AxisCount { get; }

        /// <summary>
        /// Retrieves one of this polygon's vertices.
        /// </summary>
        /// <param name="index">The index of the vertex to retrieve.</param>
        /// <returns>The vertex.</returns>
        Vector2 GetVertex(int index);

        /// <summary>
        /// Retrieves one of this polygon's axes.
        /// </summary>
        /// <param name="index">The index of the axis to retrieve.</param>
        /// <returns>The axis.</returns>
        Axis GetAxis(int index);
    }
}
