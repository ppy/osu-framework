// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    public class LinearBatch<T> : VertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly PrimitiveType type;

        [Obsolete("Use `LinearBatch(int size, PrimitiveType type)` instead.")] // Can be removed 2022-11-09
        // ReSharper disable once UnusedParameter.Local
        public LinearBatch(int size, int maxBuffers, PrimitiveType type)
            : this(size, type)
        {
        }

        public LinearBatch(int size, PrimitiveType type)
            : base(size)
        {
            this.type = type;
        }

        protected override VertexBuffer<T> CreateVertexBuffer() => new LinearVertexBuffer<T>(Size, type, BufferUsageHint.DynamicDraw);
    }
}
