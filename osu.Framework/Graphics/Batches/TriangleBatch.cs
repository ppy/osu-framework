// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// A batch to be used when drawing triangles with <see cref="TextureGLSingle.DrawTriangle"/>.
    /// </summary>
    public class TriangleBatch<T> : VertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        public TriangleBatch(int size, int maxBuffers)
            : base(size, maxBuffers)
        {
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new TriangleVertexBuffer(Size, BufferUsageHint.DynamicDraw);

        private class TriangleVertexBuffer : LinearVertexBuffer<T>
        {
            public TriangleVertexBuffer(int size, BufferUsageHint dynamicDraw)
                : base(size * TextureGLSingle.VERTICES_PER_TRIANGLE, PrimitiveType.Triangles, dynamicDraw)
            {
            }
        }
    }
}
