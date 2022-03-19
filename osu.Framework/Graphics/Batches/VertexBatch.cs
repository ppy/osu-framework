// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches
{
    public abstract class VertexBatch<T> : IVertexBatch, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public List<VertexBuffer<T>> VertexBuffers = new List<VertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentBufferIndex;
        private int currentVertexIndex;
        private int rollingVertexIndex;
        private ulong frameIndex;

        private readonly int maxBuffers;

        private VertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        protected VertexBatch(int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;
            this.maxBuffers = maxBuffers;
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
                foreach (VertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
            }
        }

        #endregion

        public void ResetCounters()
        {
            changeBeginIndex = -1;
            currentBufferIndex = 0;
            currentVertexIndex = 0;
            rollingVertexIndex = 0;
            frameIndex++;
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void AddVertex(T v)
        {
            GLWrapper.SetActiveBatch(this);

            if (currentBufferIndex < VertexBuffers.Count && currentVertexIndex >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());

            if (currentVertexBuffer.SetVertex(currentVertexIndex, v))
            {
                if (changeBeginIndex == -1)
                    changeBeginIndex = currentVertexIndex;

                changeEndIndex = currentVertexIndex + 1;
            }

            ++currentVertexIndex;
            ++rollingVertexIndex;
        }

        public int Draw()
        {
            if (currentVertexIndex == 0)
                return 0;

            VertexBuffer<T> vertexBuffer = currentVertexBuffer;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(0, currentVertexIndex);

            int count = currentVertexIndex;

            // When using multiple buffers we advance to the next one with every draw to prevent contention on the same buffer with future vertex updates.
            //TODO: let us know if we exceed and roll over to zero here.
            currentBufferIndex = (currentBufferIndex + 1) % maxBuffers;
            currentVertexIndex = 0;
            changeBeginIndex = -1;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }

        void IVertexBatch.Advance()
        {
            GLWrapper.SetActiveBatch(this);

            if (currentBufferIndex < VertexBuffers.Count && currentVertexIndex >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());

            ++currentVertexIndex;
            ++rollingVertexIndex;
        }

        public ref VertexBatchUsage<T> BeginUsage(ref VertexBatchUsage<T> usage, DrawNode node)
        {
            bool drawRequired =
                // If this is a new usage...
                usage.Batch != this
                // Or the DrawNode was newly invalidated...
                || usage.InvalidationID != node.InvalidationID
                // Or another DrawNode was inserted (and drew vertices) before this one...
                || usage.StartIndex != rollingVertexIndex
                // Or this usage is more than 1 frame behind. For example, another DrawNode may have temporarily overwritten the vertices of this one in the batch.
                || node.DrawIndex - usage.DrawIndex > 1;

            // Some DrawNodes (e.g. PathDrawNode) can reuse the same usage in multiple passes. Attempt to allow this use case.
            if (usage.Batch == this && usage.FrameIndex == frameIndex)
            {
                // Only allowed as long as the batch's current vertex index is at the end of the usage (no other usage happened in-between).
                if (rollingVertexIndex != usage.StartIndex + usage.Count)
                    throw new InvalidOperationException("Todo:");

                return ref usage;
            }

            if (drawRequired)
            {
                usage = new VertexBatchUsage<T>(
                    this,
                    node.InvalidationID,
                    rollingVertexIndex);
            }

            usage.DrawRequired = drawRequired;
            usage.DrawIndex = node.DrawIndex;
            usage.FrameIndex = frameIndex;

            return ref usage;
        }
    }

    public struct VertexBatchUsage<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        internal readonly VertexBatch<T> Batch;
        internal readonly long InvalidationID;
        internal readonly int StartIndex;

        internal ulong DrawIndex;
        internal ulong FrameIndex;
        internal bool DrawRequired;
        internal int Count;

        public VertexBatchUsage(VertexBatch<T> batch, long invalidationID, int startIndex)
        {
            Batch = batch;
            InvalidationID = invalidationID;
            StartIndex = startIndex;

            DrawIndex = 0;
            FrameIndex = 0;
            DrawRequired = false;
            Count = 0;
        }

        public void Add(T vertex)
        {
            if (DrawRequired)
                Batch.AddVertex(vertex);
            else
                ((IVertexBatch)Batch).Advance();

            Count++;
        }

        public void Dispose()
        {
        }
    }
}
