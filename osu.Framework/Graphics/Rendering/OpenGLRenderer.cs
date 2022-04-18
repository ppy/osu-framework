// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Rendering
{
    // Todo: Eventually replace GLWrapper with this.
    public class OpenGLRenderer : IRenderer
    {
        private readonly Stack<QuadBatch<TexturedVertex2D>> quadBatches = new Stack<QuadBatch<TexturedVertex2D>>();

        void IRenderer.Reset()
        {
            quadBatches.Clear();
        }

        void IRenderer.PushQuadBatch(QuadBatch<TexturedVertex2D> quadBatch)
        {
            quadBatches.Push(quadBatch);
        }

        void IRenderer.PopQuadBatch()
        {
            quadBatches.Pop();
        }

        public VertexGroupUsage<TexturedVertex2D> BeginQuads(DrawNode drawNode, VertexGroup<TexturedVertex2D> vertices)
            => quadBatches.Peek().BeginUsage(drawNode, vertices);
    }
}
