// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    public interface IPolygon
    {
        /// <summary>
        /// The vertices for this polygon that define the axes spanned by the polygon.
        /// </summary>
        /// <remarks>
        /// Must be returned in a clockwise orientation. For best performance, colinear edges should not be included.
        /// </remarks>
        ReadOnlySpan<Vector2> GetAxisVertices();

        /// <summary>
        /// Retrieves the vertices of this polygon.
        /// </summary>
        /// <remarks>
        /// Must be returned in a clockwise orientation.
        /// </remarks>
        /// <returns>The vertices of this polygon.</returns>
        ReadOnlySpan<Vector2> GetVertices();
    }
}
