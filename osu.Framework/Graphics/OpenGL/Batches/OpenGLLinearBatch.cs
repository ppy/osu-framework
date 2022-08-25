// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    internal class OpenGLLinearBatch<T> : OpenGLVertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly PrimitiveType type;

        public OpenGLLinearBatch(OpenGLRenderer renderer, int size, int maxBuffers, PrimitiveType type)
            : base(renderer, size, maxBuffers)
        {
            this.type = type;
        }

        protected override OpenGLVertexBuffer<T> CreateVertexBuffer(OpenGLRenderer renderer) => new OpenGLLinearBuffer<T>(renderer, Size, type, BufferUsageHint.DynamicDraw);
    }
}
