// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridVertexBuffer<T> : IVeldridVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;
        private IStagingBuffer<DepthWrappingVertex<T>>? stagingBuffer;

        private DeviceBuffer? buffer;

        DeviceBuffer IVeldridVertexBuffer<T>.Buffer => buffer ?? throw new InvalidOperationException("The buffer is not initialised yet.");

        private int lastWrittenVertexIndex = -1;

        /// <summary>
        /// Gets the number of vertices in this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        public VeldridVertexBuffer(VeldridRenderer renderer, int amountVertices)
        {
            this.renderer = renderer;

            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref getMemory()[vertexIndex];

            bool isNewVertex = vertexIndex > lastWrittenVertexIndex
                               || !currentVertex.Vertex.Equals(vertex)
                               || currentVertex.BackbufferDrawDepth != renderer.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = renderer.BackbufferDrawDepth;

            lastWrittenVertexIndex = Math.Max(lastWrittenVertexIndex, vertexIndex);

            return isNewVertex;
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            if (buffer == null)
                initialiseGpuBuffer();

            Debug.Assert(stagingBuffer != null);
            Debug.Assert(buffer != null);

            int countVertices = endIndex - startIndex;
            stagingBuffer.CopyTo(buffer, (uint)startIndex, (uint)startIndex, (uint)countVertices);

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        private void initialiseGpuBuffer()
        {
            ThreadSafety.EnsureDrawThread();

            getMemory();
            Debug.Assert(stagingBuffer != null);

            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(Size * IVeldridVertexBuffer<T>.STRIDE), BufferUsage.VertexBuffer | stagingBuffer.CopyTargetUsageFlags));
            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);
        }

        private Span<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                stagingBuffer = renderer.CreateStagingBuffer<DepthWrappingVertex<T>>((uint)Size);
                renderer.RegisterVertexBufferUse(this);
            }

            LastUseFrameIndex = renderer.FrameIndex;

            return stagingBuffer!.Data;
        }

        ~VeldridVertexBuffer()
        {
            renderer.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            renderer.ScheduleDisposal(v => v.Dispose(true), this);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            ((IVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public ulong LastUseFrameIndex { get; private set; }

        public bool InUse => LastUseFrameIndex > 0;

        public void Free()
        {
            memoryLease?.Dispose();
            memoryLease = null;

            stagingBuffer?.Dispose();
            stagingBuffer = null;

            buffer?.Dispose();
            buffer = null;

            LastUseFrameIndex = 0;
        }
    }
}
