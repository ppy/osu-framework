// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    internal class OpenGLQuadBatch<T> : OpenGLVertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        public OpenGLQuadBatch(OpenGLRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
        }

        protected override OpenGLVertexBuffer<T> CreateVertexBuffer(OpenGLRenderer renderer) => new OpenGLQuadBuffer<T>(renderer, Size, BufferUsageHint.DynamicDraw);
    }
}
