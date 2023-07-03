// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal abstract class VeldridVertexBatch<T> : IVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly List<VeldridVertexBuffer<T>>[] tripleBufferedVertexBuffers = new List<VeldridVertexBuffer<T>>[3];

        public List<VeldridVertexBuffer<T>> CurrentVertexBuffers
        {
            get => tripleBufferedVertexBuffers[renderer.ResetId % 3];
            set => tripleBufferedVertexBuffers[renderer.ResetId % 3] = value;
        }

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentBufferIndex;
        private int currentVertexIndex;
        private int currentDrawIndex;

        private readonly VeldridRenderer renderer;

        private VeldridVertexBuffer<T>? currentVertexBuffer => CurrentVertexBuffers.Count == 0 ? null : CurrentVertexBuffers[currentBufferIndex];

        protected VeldridVertexBatch(VeldridRenderer renderer, int bufferSize)
        {
            Size = bufferSize;
            this.renderer = renderer;

            AddAction = Add;

            for (int i = 0; i < tripleBufferedVertexBuffers.Length; i++)
                tripleBufferedVertexBuffers[i] = new List<VeldridVertexBuffer<T>>();
        }

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < tripleBufferedVertexBuffers.Length; i++)
                {
                    foreach (VeldridVertexBuffer<T> vbo in tripleBufferedVertexBuffers[i])
                        vbo.Dispose();
                }
            }
        }

        #endregion

        void IVertexBatch.ResetCounters()
        {
            changeBeginIndex = -1;
            currentBufferIndex = 0;
            currentVertexIndex = 0;
            currentDrawIndex = 0;
        }

        protected abstract VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer);

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        // todo: this might break when the vertices that are about to be added cannot all fit into this buffer.
        public void Add(T v)
        {
            renderer.SetActiveBatch(this);

            if (currentVertexBuffer != null && currentVertexIndex >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);

                currentBufferIndex++;
                currentVertexIndex = 0;
                currentDrawIndex = 0;
            }

            if (currentBufferIndex >= CurrentVertexBuffers.Count)
                CurrentVertexBuffers.Add(CreateVertexBuffer(renderer));

            if (currentVertexBuffer!.SetVertex(currentVertexIndex, v))
            {
                if (changeBeginIndex == -1)
                    changeBeginIndex = currentVertexIndex;

                changeEndIndex = currentVertexIndex + 1;
            }

            ++currentVertexIndex;
        }

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// This is a cached delegate of <see cref="Add"/> that should be used in memory-critical locations such as <see cref="DrawNode"/>s.
        /// </summary>
        public Action<T> AddAction { get; private set; }

        public int Draw()
        {
            if (currentVertexIndex == currentDrawIndex)
                return 0;

            VeldridVertexBuffer<T> vertexBuffer = currentVertexBuffer!;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(currentDrawIndex, currentVertexIndex);

            int count = currentVertexIndex - currentDrawIndex;

            // When using multiple buffers we advance to the next one with every draw to prevent contention on the same buffer with future vertex updates.
            currentDrawIndex = currentVertexIndex;
            changeBeginIndex = -1;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }
    }
}
