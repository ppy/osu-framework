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

        private int currentBufferIndex;
        private int rollingVertexIndex;

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
            currentBufferIndex = 0;
            rollingVertexIndex = 0;
            drawStart = 0;
            drawCount = 0;
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        private int drawStart;
        private int drawCount;

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void AddVertex(T v)
        {
            ensureHasBufferSpace();
            currentVertexBuffer.EnqueueVertex(drawStart + drawCount, v);

#if VBO_CONSISTENCY_CHECKS
            Trace.Assert(GetCurrentVertex().Equals(v));
#endif

            Advance();
        }

        internal void Advance()
        {
            ++drawCount;
            ++rollingVertexIndex;
        }

#if VBO_CONSISTENCY_CHECKS
        internal T GetCurrentVertex()
        {
            ensureHasBufferSpace();
            return VertexBuffers[currentBufferIndex].Vertices[drawStart + drawCount].Vertex;
        }
#endif

        public int Draw()
        {
            int count = drawCount;

            while (drawCount > 0)
            {
                int drawEnd = Math.Min(currentVertexBuffer.Size, drawStart + drawCount);
                int currentDrawCount = drawEnd - drawStart;

                currentVertexBuffer.DrawRange(drawStart, drawEnd);
                drawStart += currentDrawCount;
                drawCount -= currentDrawCount;

                if (drawStart == currentVertexBuffer.Size)
                {
                    drawStart = 0;
                    currentBufferIndex++;
                }

                FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
                FrameStatistics.Add(StatisticsCounterType.VerticesDraw, currentDrawCount);
            }

            return count;
        }

        private void ensureHasBufferSpace()
        {
            if (VertexBuffers.Count > currentBufferIndex && drawStart + drawCount >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());
        }

        public ref VertexBatchUsage<T> BeginUsage(ref VertexBatchUsage<T> usage, DrawNode node)
        {
            GLWrapper.SetActiveBatch(this);

            ulong frameIndex = GLWrapper.DrawNodeFrameIndices[GLWrapper.ResetIndex];

            bool drawRequired =
                // If this is a new usage.
                usage.Batch != this
                // Or the DrawNode was newly invalidated.
                || usage.InvalidationID != node.InvalidationID
                // Or another DrawNode was inserted (and drew vertices) before this one.
                || usage.StartIndex != rollingVertexIndex
                // Or this usage has been skipped for 1 frame. Another DrawNode may have temporarily overwritten the vertices of this one in the batch.
                || frameIndex - usage.FrameIndex > 1
                // Or if this node has a different backbuffer draw depth (the DrawNode structure changed elsewhere in the scene graph).
                || node.DrawDepth != usage.DrawDepth;

            // Some DrawNodes (e.g. PathDrawNode) can reuse the same usage in multiple passes. Attempt to allow this use case.
            if (usage.Batch == this && frameIndex > 0 && usage.FrameIndex == frameIndex)
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
                    rollingVertexIndex,
                    node.DrawDepth);
            }

            usage.FrameIndex = frameIndex;
            usage.DrawRequired = drawRequired;

            return ref usage;
        }
    }

    public struct VertexBatchUsage<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        internal readonly VertexBatch<T> Batch;
        internal readonly long InvalidationID;
        internal readonly int StartIndex;
        internal readonly float DrawDepth;

        internal ulong FrameIndex;
        internal bool DrawRequired;
        internal int Count;

        public VertexBatchUsage(VertexBatch<T> batch, long invalidationID, int startIndex, float drawDepth)
        {
            Batch = batch;
            InvalidationID = invalidationID;
            StartIndex = startIndex;
            DrawDepth = drawDepth;

            FrameIndex = 0;
            DrawRequired = false;
            Count = 0;
        }

        public void Add(T vertex)
        {
            if (DrawRequired)
                Batch.AddVertex(vertex);
            else
            {
#if VBO_CONSISTENCY_CHECKS
                if (!Batch.GetCurrentVertex().Equals(vertex))
                    throw new InvalidOperationException("Vertex draw was skipped, but the contained vertex differs.");
#endif

                Batch.Advance();
            }

            Count++;
        }

        public void Dispose()
        {
        }
    }
}
