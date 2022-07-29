// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        public const int VERTICES_PER_QUAD = 4;

        public const int VERTICES_PER_TRIANGLE = 4;

        /// <summary>
        /// Creates a new linear vertex batch, accepting vertices and drawing as a given primitive type.
        /// </summary>
        /// <param name="size">Number of quads.</param>
        /// <param name="maxBuffers">Maximum number of vertex buffers.</param>
        /// <param name="primitiveType">The type of primitive the vertices are drawn as.</param>
        IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveType primitiveType) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;

        /// <summary>
        /// Creates a new quad vertex batch, accepting vertices and drawing as quads.
        /// </summary>
        /// <param name="size">Number of quads.</param>
        /// <param name="maxBuffers">Maximum number of vertex buffers.</param>
        IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;
    }
}
