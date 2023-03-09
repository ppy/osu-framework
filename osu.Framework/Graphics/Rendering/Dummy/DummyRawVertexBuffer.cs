// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Dummy
{
    internal class DummyRawVertexBuffer<TVertex> : IRawVertexBuffer<TVertex> where TVertex : unmanaged, IVertex
    {
        public void SetLayout()
        {
        }

        public void SetLayout(ReadOnlySpan<int> layoutPositions)
        {
        }

        public void Draw(PrimitiveTopology topology, int count, int offset = 0)
        {
        }

        public void SetCapacity(int size, BufferUsageHint usageHint)
        {
        }

        public void BufferData(ReadOnlySpan<TVertex> data, BufferUsageHint usageHint)
        {
        }

        public void UpdateRange(ReadOnlySpan<TVertex> data, int offset = 0)
        {
        }

        public bool Bind()
        {
            return true;
        }

        public void Unbind()
        {
        }

        public void Dispose()
        {
        }
    }
}
