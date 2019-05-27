// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    public interface IPolygon
    {
        /// <summary>
        /// The vertices that define the axes spanned by this polygon.
        /// </summary>
        /// <remarks>
        /// Must be returned in a clockwise orientation. For best performance, vertices that form colinear edges should not be included.
        /// </remarks>
        /// <returns>
        /// The vertices that define the axes spanned by this polygon.
        /// </returns>
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
