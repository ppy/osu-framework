// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    public enum VeldridIndexLayout
    {
        /// <summary>
        /// An index layout that draws the vertices in the order they are added.
        /// </summary>
        Linear,

        /// <summary>
        /// An index layout that transforms every 6 vertices to a single quad.
        /// </summary>
        Quad,
    }
}
