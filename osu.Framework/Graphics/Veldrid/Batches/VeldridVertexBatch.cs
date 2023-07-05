// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal abstract class VeldridVertexBatch<T> : IVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        /// <summary>
        /// Most documentation recommends that three buffers are used to avoid contention.
        ///
        /// We already have a triple buffer (see <see cref="GameHost.DrawRoots"/>) at a higher level which guarantees one extra previous buffer,
        /// so setting this to two here is ample to guarantee we don't hit any weird edge cases (gives a theoretical buffer count of 4, in the worst scenario).
        ///
        /// Note that due to the higher level triple buffer, the actual number of buffers we are storing is three times as high as this constant.
        /// Maintaining this many buffers is a cause of concern from an memory alloc / GPU upload perspective.
        /// </summary>
        private const int vertex_buffer_count = 2;

        /// <summary>
        /// Multiple VBOs in a swap chain to try our best to avoid GPU contention.
        /// </summary>
        private readonly List<VeldridVertexBuffer<T>>[] vertexBuffers = new List<VeldridVertexBuffer<T>>[FrameworkEnvironment.VertexBufferCount ?? vertex_buffer_count];

        private List<VeldridVertexBuffer<T>> currentVertexBuffers => vertexBuffers[renderer.ResetId % (ulong)vertexBuffers.Length];

        private VeldridVertexBuffer<T> currentVertexBuffer => currentVertexBuffers[currentBufferIndex];

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentBufferIndex;
        private int currentVertexIndex;

        private readonly VeldridRenderer renderer;

        protected VeldridVertexBatch(VeldridRenderer renderer, int bufferSize)
        {
            Size = bufferSize;
            this.renderer = renderer;

            AddAction = Add;

            for (int i = 0; i < vertexBuffers.Length; i++)
                vertexBuffers[i] = new List<VeldridVertexBuffer<T>>();
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
                for (int i = 0; i < vertexBuffers.Length; i++)
                {
                    foreach (VeldridVertexBuffer<T> vbo in vertexBuffers[i])
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
        }

        protected abstract VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer);

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void Add(T v)
        {
            renderer.SetActiveBatch(this);

            if (currentBufferIndex < currentVertexBuffers.Count && currentVertexIndex >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            while (currentBufferIndex >= currentVertexBuffers.Count)
                currentVertexBuffers.Add(CreateVertexBuffer(renderer));

            if (currentVertexBuffer.SetVertex(currentVertexIndex, v))
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
            if (currentVertexIndex == 0)
                return 0;

            VeldridVertexBuffer<T> vertexBuffer = currentVertexBuffer;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(0, currentVertexIndex);

            int count = currentVertexIndex;

            // When using multiple buffers we advance to the next one with every draw to prevent contention on the same buffer with future vertex updates.
            currentBufferIndex++;
            currentVertexIndex = 0;
            changeBeginIndex = -1;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }
    }
}
