// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal class VeldridQuadBatch<T> : VeldridVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public VeldridQuadBatch(VeldridRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
            if (size > VeldridQuadBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(VeldridQuadBuffer<T>)} with more than {nameof(VeldridQuadBuffer<T>)}.{nameof(VeldridQuadBuffer<T>.MAX_QUADS)} quads ({VeldridQuadBuffer<T>.MAX_QUADS}).");
        }

        protected override VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer) => new VeldridQuadBuffer<T>(renderer, Size, BufferUsage.Dynamic);
    }
}
