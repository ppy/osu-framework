// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private readonly VertexBufferList<T> vertexBufferList;

        protected VertexBatch(int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;

            vertexBufferList = new VertexBufferList<T>(maxBuffers, () => CreateVertexBuffer());
            vertexBufferList.OnCommit += draw;
        }

        public void Dispose()
        {
            vertexBufferList.Dispose();
        }

        public void ResetCounters()
        {
            vertexBufferList.Reset();
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        private bool groupActuallyBegun;
        private bool groupInUse;
        private bool wasOverflowed;

        void IVertexBatch.Add<TInput>(IVertexGroup vertices, TInput vertex) => vertexBufferList.Push(vertices.Transform<TInput, T>(vertex));

        void IVertexBatch.Advance(int count) => vertexBufferList.Advance(count);

        void IVertexBatch.UsageStarted(IVertexGroup group)
        {
            groupInUse = true;

            groupActuallyBegun = false;
            group.TriggeredOverflow = false;
            wasOverflowed = vertexBufferList.ThisDrawHasOverflowVertices;
        }

        void IVertexBatch.UsageFinished(IVertexGroup group)
        {
            groupInUse = false;

            group.TriggeredOverflow = !wasOverflowed && vertexBufferList.ThisDrawHasOverflowVertices;
            wasOverflowed = false;
        }

        void IVertexBatch.ActuallyBeginUsage(IVertexGroup group)
        {
            if (groupActuallyBegun)
                return;

            updateGroup(group);
            groupActuallyBegun = true;
        }

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        void IVertexBatch.AssertIsCurrentVertex<TVertex>(IVertexGroup vertices, TVertex vertex, string failureMessage)
            => vertexBufferList.AssertIsCurrentVertex(vertices.Transform<TVertex, T>(vertex), failureMessage);
#endif

        public void Draw() => vertexBufferList.Commit();

        private void draw(VertexBuffer<T> buffer)
        {
            int countToDraw = buffer.Count;

            buffer.Draw();

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, countToDraw);
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
            if (((IVertexGroup)vertices).FrameIndex == frameIndex)
                throw new InvalidOperationException($"A {nameof(VertexGroup<T>)} cannot be used multiple times within a single frame.");

            // Disallow nested usages.
            if (groupInUse)
                throw new InvalidOperationException($"Nesting of {nameof(VertexGroup<T>)}s is not allowed.");

            GLWrapper.SetActiveBatch(this);
            ((IVertexGroup)vertices).DrawNode = drawNode;

            return new VertexGroupUsage<TInput>(this, vertices);
        }

        private void updateGroup(IVertexGroup group)
        {
            Debug.Assert(group.DrawNode != null);

            ulong frameIndex = GLWrapper.CurrentTreeResetId;

            // Make sure to test in DEBUG when changing the following heuristics.
            bool uploadRequired =
                // If this is a new usage or has been moved to a new batch.
                group.Batch != this
                // Or the DrawNode was moved around the vertex list.
                || group.BufferIndex != vertexBufferList.CurrentBufferIndex
                || group.VertexIndex != vertexBufferList.CurrentVertexIndex
                // Or this usage has been skipped for 1 frame. Another DrawNode may have overwritten the vertices of this one in the batch.
                // Todo: This check is probably redundant with the one below.
                || frameIndex - group.FrameIndex > 1
                // Or this group was the one that triggered an overflow in the last frame. Some (or all) vertices will need to be redrawn.
                || group.TriggeredOverflow
                // Or the vertex buffer was overflowed into in the last frame.
                || vertexBufferList.LastDrawHadOverflowVertices
                // Or the DrawNode was newly invalidated.
                || group.InvalidationID != group.DrawNode.InvalidationID
                // Or if this node has a different backbuffer draw depth (the DrawNode structure changed elsewhere in the scene graph).
                || group.DrawDepth != group.DrawNode.DrawDepth;

            group.Batch = this;
            group.BufferIndex = vertexBufferList.CurrentBufferIndex;
            group.VertexIndex = vertexBufferList.CurrentVertexIndex;
            group.FrameIndex = frameIndex;
            group.InvalidationID = group.DrawNode.InvalidationID;
            group.DrawDepth = group.DrawNode.DrawDepth;
            group.UploadRequired = uploadRequired;
        }
    }
}
