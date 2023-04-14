// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
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
        private readonly DeviceBuffer buffer;

        private readonly DeviceBuffer stagingBuffer;
        private readonly MappedResource stagingResource;

        // todo: need MemoryManager<T> for unmanaged resources to use Memory<T> instead of a pointer and make this safe.
        private readonly unsafe DepthWrappingVertex<T>* memory;
        private readonly NativeMemoryTracker.NativeMemoryLease memoryLease;

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        /// <summary>
        /// Gets the number of vertices in this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        protected abstract PrimitiveTopology Type { get; }

        protected unsafe VeldridVertexBuffer(VeldridRenderer renderer, int amountVertices, BufferUsage usage)
        {
            this.renderer = renderer;

            Size = amountVertices;

            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(Size * STRIDE), BufferUsage.VertexBuffer | usage));
            stagingBuffer = renderer.Factory.CreateBuffer(new BufferDescription(buffer.SizeInBytes, BufferUsage.Staging));
            stagingResource = renderer.Device.Map(stagingBuffer, MapMode.ReadWrite);

            memory = (DepthWrappingVertex<T>*)stagingResource.Data.ToPointer();
            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);

            Unsafe.InitBlock(memory, 0, buffer.SizeInBytes);
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public unsafe bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref memory[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex) || currentVertex.BackbufferDrawDepth != renderer.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = renderer.BackbufferDrawDepth;

            return isNewVertex;
        }

        public virtual void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            Debug.Assert(buffer != null);
            renderer.BindVertexBuffer(buffer, VeldridVertexUtils<DepthWrappingVertex<T>>.Layout);
        }

        public virtual void Unbind()
        {
        }

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            Bind();

            int countVertices = endIndex - startIndex;
            renderer.DrawVertices(Type, ToElementIndex(startIndex), ToElements(countVertices));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            int countVertices = endIndex - startIndex;
            renderer.BufferUpdateCommands.CopyBuffer(stagingBuffer, (uint)(startIndex * STRIDE), buffer, (uint)(startIndex * STRIDE), (uint)(countVertices * STRIDE));

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        public void Free()
        {
            renderer.Device.Unmap(stagingResource.Resource);

            memoryLease.Dispose();
            buffer.Dispose();
            stagingBuffer.Dispose();
            LastUseResetId = 0;
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
    }
}
