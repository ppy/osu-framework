// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal class VeldridLinearBatch<T> : VeldridVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveTopology type;

        public VeldridLinearBatch(VeldridRenderer renderer, int size, int maxBuffers, PrimitiveTopology type)
            : base(renderer, size, maxBuffers)
        {
            this.type = type;
        }

        protected override VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer) => new VeldridLinearBuffer<T>(renderer, Size, type, BufferUsage.Dynamic);
    }
}
