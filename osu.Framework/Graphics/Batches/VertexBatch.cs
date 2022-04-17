// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Batches.Internal;
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

        private VertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        [Obsolete("Use `VertexBatch(int bufferSize)` instead.")] // Can be removed 2022-11-09
        // ReSharper disable once UnusedParameter.Local
        protected VertexBatch(int bufferSize, int maxBuffers)
            : this(bufferSize)
        {
        }

        protected VertexBatch(int bufferSize)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;
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

        private bool groupInUse;
        private int drawStart;
        private int drawCount;

        void IVertexBatch.Add<TInput>(IVertexGroup vertices, TInput vertex)
        {
            ensureHasBufferSpace();
            currentVertexBuffer.EnqueueVertex(drawStart + drawCount, vertices.Transform<TInput, T>(vertex));

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            ((IVertexBatch)this).EnsureCurrentVertex(vertices, vertex, "Added vertex does not equal the given one. Vertex equality comparer is probably broken.");
#endif

            advance(1);
        }

        void IVertexBatch.Advance(int count) => advance(count);

        void IVertexBatch.UsageStarted() => groupInUse = true;

        void IVertexBatch.UsageFinished() => groupInUse = false;

        private void advance(int count)
        {
            drawCount += count;
            rollingVertexIndex += count;
        }

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        void IVertexBatch.EnsureCurrentVertex<TVertex>(IVertexGroup vertices, TVertex vertex, string failureMessage)
        {
            ensureHasBufferSpace();

            if (!VertexBuffers[currentBufferIndex].Vertices[drawStart + drawCount].Vertex.Equals(vertices.Transform<TVertex, T>(vertex)))
                throw new InvalidOperationException(failureMessage);
        }
#endif

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

        /// <summary>
        /// Begins a grouping of vertices.
        /// </summary>
        /// <param name="drawNode">The owner of the vertices.</param>
        /// <param name="vertices">The grouping of vertices.</param>
        /// <returns>A usage of the <see cref="VertexGroup{TVertex}"/>.</returns>
        /// <exception cref="InvalidOperationException">When the same <see cref="VertexGroup{TVertex}"/> is used multiple times in a single draw frame.</exception>
        /// <exception cref="InvalidOperationException">When attempting to nest <see cref="VertexGroup{TVertex}"/> usages.</exception>
        public VertexGroupUsage<TInput> BeginUsage<TInput>(DrawNode drawNode, VertexGroup<TInput, T> vertices)
            where TInput : struct, IEquatable<TInput>, IVertex
        {
            ulong frameIndex = GLWrapper.CurrentTreeResetId;

            // Disallow reusing the same group multiple times in the same draw frame.
            if (vertices.Batch == this && frameIndex > 0 && vertices.FrameIndex == frameIndex)
                throw new InvalidOperationException($"A {nameof(VertexGroup<T>)} cannot be used multiple times within a single frame.");

            // Disallow nested usages.
            if (groupInUse)
                throw new InvalidOperationException($"Nesting of {nameof(VertexGroup<T>)}s is not allowed.");

            GLWrapper.SetActiveBatch(this);

            // Make sure to test in DEBUG when changing the following heuristics.
            bool uploadRequired =
                // If this is a new usage or has been moved to a new batch.
                vertices.Batch != this
                // Or the DrawNode was newly invalidated.
                || vertices.InvalidationID != drawNode.InvalidationID
                // Or another DrawNode was inserted (and drew vertices) before this one.
                || vertices.StartIndex != rollingVertexIndex
                // Or this usage has been skipped for 1 frame. Another DrawNode may have temporarily overwritten the vertices of this one in the batch.
                || frameIndex - vertices.FrameIndex > 1
                // Or if this node has a different backbuffer draw depth (the DrawNode structure changed elsewhere in the scene graph).
                || drawNode.DrawDepth != vertices.DrawDepth;

            vertices.Batch = this;
            vertices.InvalidationID = drawNode.InvalidationID;
            vertices.StartIndex = rollingVertexIndex;
            vertices.DrawDepth = drawNode.DrawDepth;
            vertices.FrameIndex = frameIndex;

            return new VertexGroupUsage<TInput>(this, vertices, uploadRequired);
        }
    }
}
