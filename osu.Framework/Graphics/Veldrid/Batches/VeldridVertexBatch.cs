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
        /// We already have a triple buffer (see <see cref="GameHost.DrawRoots"/>) governing draw nodes.
        /// In theory we could set this to two, but there's also a global usage of a vertex batch in <see cref="VeldridRenderer"/> (see <see cref="VeldridRenderer.DefaultQuadBatch"/>).
        ///
        /// So this is for now an unfortunate memory overhead. Further work could be done to provide
        /// these in a way they were not created per draw-node, reducing buffer overhead from 9 to 3.
        /// </summary>
        private const int vertex_buffer_count = 3;

        /// <summary>
        /// Multiple VBOs in a swap chain to try our best to avoid GPU contention.
        /// </summary>
        private readonly List<VeldridVertexBuffer<T>>[] vertexBuffers = new List<VeldridVertexBuffer<T>>[FrameworkEnvironment.VertexBufferCount ?? vertex_buffer_count];

        private List<VeldridVertexBuffer<T>> currentVertexBuffers => vertexBuffers[renderer.FrameIndex % (ulong)vertexBuffers.Length];

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
            currentDrawIndex = 0;
        }

        protected abstract VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer);

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void Add(T v)
        {
            renderer.SetActiveBatch(this);

            var buffers = currentVertexBuffers;

            if (buffers.Count > 0 && currentVertexIndex >= buffers[currentBufferIndex].Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);

                currentBufferIndex++;
                currentVertexIndex = 0;
                currentDrawIndex = 0;
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            if (currentBufferIndex >= buffers.Count)
                buffers.Add(CreateVertexBuffer(renderer));

            if (buffers[currentBufferIndex].SetVertex(currentVertexIndex, v))
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

            var buffers = currentVertexBuffers;

            if (buffers.Count == 0)
                return 0;

            VeldridVertexBuffer<T> buffer = buffers[currentBufferIndex];

            if (changeBeginIndex >= 0)
                buffer.UpdateRange(changeBeginIndex, changeEndIndex);

            buffer.DrawRange(currentDrawIndex, currentVertexIndex);

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
