// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Draws to the screen.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Resets this <see cref="IRenderer"/> for a new frame.
        /// </summary>
        internal void Reset();

        /// <summary>
        /// Pushes a quad batch to be used for calls to <see cref="BeginQuads"/>.
        /// </summary>
        /// <param name="quadBatch">The <see cref="QuadBatch{T}"/>.</param>
        internal void PushQuadBatch(QuadBatch<TexturedVertex2D> quadBatch);

        /// <summary>
        /// Pops a quad batch.
        /// </summary>
        internal void PopQuadBatch();

        // Todo: This should eventually be replaced with a "BeginVertices" method instead.
        /// <summary>
        /// Begins a grouping of vertices. Each quad requires <see cref="TextureGLSingle.VERTICES_PER_QUAD"/> to be fully formed.
        /// </summary>
        /// <param name="drawNode">The owner of the vertices.</param>
        /// <param name="vertices">The grouping of vertices.</param>
        /// <returns>A usage of the <see cref="VertexGroup{TVertex}"/>.</returns>
        /// <exception cref="InvalidOperationException">When the same <see cref="VertexGroup{TVertex}"/> is used multiple times in a single draw frame.</exception>
        /// <exception cref="InvalidOperationException">When attempting to nest <see cref="VertexGroup{TVertex}"/> usages.</exception>
        VertexGroupUsage<TexturedVertex2D> BeginQuads(DrawNode drawNode, VertexGroup<TexturedVertex2D> vertices);
    }
}
