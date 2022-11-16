// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    internal class GLLinearBatch<T> : GLVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveType type;

        public GLLinearBatch(GLRenderer renderer, int size, int maxBuffers, PrimitiveType type)
            : base(renderer, size, maxBuffers)
        {
            this.type = type;
        }

        protected override GLVertexBuffer<T> CreateVertexBuffer(GLRenderer renderer) => new GLLinearBuffer<T>(renderer, Size, type, BufferUsageHint.DynamicDraw);
    }
}
