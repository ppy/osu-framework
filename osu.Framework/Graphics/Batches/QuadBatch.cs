// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Batches
{
    public class QuadBatch<T> : VertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        [Obsolete("Use `QuadBatch(int size)` instead.")] // Can be removed 2022-11-09
        // ReSharper disable once UnusedParameter.Local
        public QuadBatch(int size, int maxBuffers)
            : this(size)
        {
        }

        public QuadBatch(int size)
            : base(size)
        {
            if (size > QuadVertexBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(QuadVertexBuffer<T>)} with more than {nameof(QuadVertexBuffer<T>)}.{nameof(QuadVertexBuffer<T>.MAX_QUADS)} quads ({QuadVertexBuffer<T>.MAX_QUADS}).");
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new QuadVertexBuffer<T>(Size, BufferUsageHint.DynamicDraw);
    }
}
