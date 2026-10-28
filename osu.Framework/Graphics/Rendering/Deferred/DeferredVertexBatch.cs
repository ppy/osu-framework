// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredVertexBatch<TVertex> : IVertexBatch<TVertex>, IDeferredVertexBatch
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        private static readonly TVertex[] current_primitive = new TVertex[4];

        // ReSharper disable once StaticMemberInGenericType
        private static int currentPrimitiveSize;

        public Action<TVertex> AddAction { get; }

        private readonly DeferredRenderer renderer;
        private readonly PrimitiveTopology topology;
        private readonly VeldridIndexLayout indexLayout;
        private readonly int primitiveSize;

        private int currentDrawCount;

        public DeferredVertexBatch(DeferredRenderer renderer, PrimitiveTopology topology, VeldridIndexLayout indexLayout)
        {
            this.renderer = renderer;

            this.topology = topology;
            this.indexLayout = indexLayout;

            if (this.indexLayout == VeldridIndexLayout.Linear)
            {
                switch (this.topology)
                {
                    case PrimitiveTopology.Points:
                        primitiveSize = 1;
                        break;

                    case PrimitiveTopology.Lines:
                        primitiveSize = 2;
                        break;

                    case PrimitiveTopology.Triangles:
                        primitiveSize = 3;
                        break;

                    case PrimitiveTopology.LineStrip:
                    case PrimitiveTopology.TriangleStrip:
                        throw new NotImplementedException($"Topology '{topology}' is not yet implemented for this renderer.");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(topology));
                }
            }
            else
                primitiveSize = 4;

            AddAction = ((IVertexBatch<TVertex>)this).Add;
        }

        public void Write(in MemoryReference primitive)
            => renderer.Context.VertexManager.Write<TVertex>(primitive);

        public void Draw(int count)
            => renderer.Context.VertexManager.Draw<TVertex>(count, topology, indexLayout, primitiveSize);

        int IVertexBatch.Size
            => int.MaxValue;

        int IVertexBatch.Draw()
        {
            int count = currentDrawCount;
            currentDrawCount = 0;

            if (count == 0)
                return 0;

            renderer.DrawVertices(topology, 0, count);
            renderer.Context.EnqueueEvent(FlushEvent.Create(renderer, this, count));
            return count;
        }

        void IVertexBatch.ResetCounters()
        {
            currentPrimitiveSize = 0;
            currentDrawCount = 0;
        }

        void IVertexBatch<TVertex>.Add(TVertex vertex)
        {
            renderer.SetActiveBatch(this);

            current_primitive[currentPrimitiveSize] = vertex;

            if (++currentPrimitiveSize == primitiveSize)
            {
                renderer.Context.EnqueueEvent(AddPrimitiveToBatchEvent.Create(renderer, this, current_primitive.AsSpan()[..primitiveSize]));
                currentPrimitiveSize = 0;
            }

            currentDrawCount++;
        }

        public void Dispose()
        {
        }
    }
}
