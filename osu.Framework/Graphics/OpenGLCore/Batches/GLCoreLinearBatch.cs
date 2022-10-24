// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGLCore.Buffers;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore.Batches
{
    internal class GLCoreLinearBatch<T> : GLCoreVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveType type;

        public GLCoreLinearBatch(GLCoreRenderer renderer, int size, int maxBuffers, PrimitiveType type)
            : base(renderer, size, maxBuffers)
        {
            this.type = type;
        }

        protected override GLCoreVertexBuffer<T> CreateVertexBuffer(GLCoreRenderer renderer) => new GLCoreLinearBuffer<T>(renderer, Size, type, BufferUsageHint.DynamicDraw);
    }
}
