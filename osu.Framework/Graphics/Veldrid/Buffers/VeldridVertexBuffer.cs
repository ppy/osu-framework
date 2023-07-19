// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Graphics.Veldrid.Vertices;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal abstract class VeldridVertexBuffer<T> : IVertexBuffer
        where T : unmanaged, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = VeldridVertexUtils<DepthWrappingVertex<T>>.STRIDE;

        private readonly VeldridRenderer renderer;

        private NativeMemoryTracker.NativeMemoryLease? memoryLease;
        private IStagingBuffer<DepthWrappingVertex<T>>? stagingBuffer;
        private DeviceBuffer? gpuBuffer;

        private int lastWrittenVertexIndex = -1;

        protected VeldridVertexBuffer(VeldridRenderer renderer, int amountVertices)
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

        /// <summary>
        /// Gets the number of vertices in this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Initialises this <see cref="VeldridVertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            getMemory();
            Debug.Assert(stagingBuffer != null);

            gpuBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(Size * STRIDE), BufferUsage.VertexBuffer | stagingBuffer.CopyTargetUsageFlags));
            memoryLease = NativeMemoryTracker.AddMemory(this, gpuBuffer.SizeInBytes);
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

        public virtual void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (gpuBuffer == null)
                Initialise();

            Debug.Assert(gpuBuffer != null);
            renderer.BindVertexBuffer(gpuBuffer, VeldridVertexUtils<DepthWrappingVertex<T>>.Layout);
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveTopology Type { get; }

        public void DrawRange(int startIndex, int endIndex)
        {
            Bind();

            int countVertices = endIndex - startIndex;
            renderer.DrawVertices(Type, ToElementIndex(startIndex), ToElements(countVertices));

            Unbind();
        }

        internal void UpdateRange(int startIndex, int endIndex)
        {
            if (gpuBuffer == null)
                Initialise();

            Debug.Assert(stagingBuffer != null);
            Debug.Assert(gpuBuffer != null);

            int countVertices = endIndex - startIndex;
            stagingBuffer.CopyTo(gpuBuffer, (uint)startIndex, (uint)startIndex, (uint)countVertices);

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
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

        public ulong LastUseFrameIndex { get; private set; }

        public bool InUse => LastUseFrameIndex > 0;

        public void Free()
        {
            memoryLease?.Dispose();
            memoryLease = null;

            stagingBuffer?.Dispose();
            stagingBuffer = null;

            gpuBuffer?.Dispose();
            gpuBuffer = null;

            LastUseFrameIndex = 0;
        }
    }
}
