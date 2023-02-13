// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGLCore.Buffers;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore.Batches
{
    internal class GLCoreQuadBatch<T> : GLCoreVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public GLCoreQuadBatch(GLCoreRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
        }

        protected override GLCoreVertexBuffer<T> CreateVertexBuffer(GLCoreRenderer renderer) => new GLCoreQuadBuffer<T>(renderer, Size, BufferUsageHint.DynamicDraw);
    }
}
