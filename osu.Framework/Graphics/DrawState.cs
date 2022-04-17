// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Represents the current renderer state while the scene is being drawn.
    /// </summary>
    public readonly ref struct DrawState
    {
        /// <summary>
        /// The quad batch of the most recent <see cref="CompositeDrawable.CompositeDrawableDrawNode"/> with a local batch.
        /// </summary>
        private readonly QuadBatch<TexturedVertex2D> quadBatch;

        /// <summary>
        /// Creates a new <see cref="DrawState"/>.
        /// </summary>
        /// <param name="quadBatch">The quad batch.</param>
        internal DrawState(QuadBatch<TexturedVertex2D> quadBatch)
        {
            this.quadBatch = quadBatch;
        }

        // Todo: This should eventually be replaced with a BeginVertices<TVertex>() method instead.
        /// <summary>
        /// Begins a grouping of quad vertices. Each quad requires <see cref="TextureGLSingle.VERTICES_PER_QUAD"/> to be fully formed.
        /// </summary>
        /// <param name="drawNode">The owner of the vertices.</param>
        /// <param name="vertices">The grouping of vertices.</param>
        /// <returns>A usage of the <see cref="VertexGroup{TVertex}"/>.</returns>
        /// <exception cref="InvalidOperationException">When the same <see cref="VertexGroup{TVertex}"/> is used multiple times in a single draw frame.</exception>
        /// <exception cref="InvalidOperationException">When attempting to nest <see cref="VertexGroup{TVertex}"/> usages.</exception>
        public VertexGroupUsage<TexturedVertex2D> BeginUsage(DrawNode drawNode, VertexGroup<TexturedVertex2D> vertices)
            => quadBatch.BeginUsage(drawNode, vertices);

        /// <summary>
        /// Creates a new <see cref="DrawState"/> with a given quad batch.
        /// </summary>
        /// <param name="quadBatch">The new quad batch.</param>
        /// <returns>The new <see cref="DrawState"/>.</returns>
        internal DrawState WithQuadBatch(QuadBatch<TexturedVertex2D> quadBatch) => new DrawState(quadBatch);
    }
}
