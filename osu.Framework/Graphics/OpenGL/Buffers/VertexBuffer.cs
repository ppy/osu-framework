// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;
using osu.Framework.Platform;
using SixLabors.Memory;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public abstract class VertexBuffer<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

        private readonly BufferUsageHint usage;

        private IMemoryOwner<DepthWrappingVertex<T>> memoryOwner;
        private Memory<DepthWrappingVertex<T>> vertexMemory;

        private bool isInitialised;
        private int vboId;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;

            memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(amountVertices, AllocationOptions.Clean);
            vertexMemory = memoryOwner.Memory;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref vertexMemory.Span[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex) || currentVertex.BackbufferDrawDepth != GLWrapper.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth;

            return isNewVertex;
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        public int Size => vertexMemory.Length;

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        /// <summary>
        /// Initialises this <see cref="VertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out vboId);

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            memoryLease = NativeMemoryTracker.AddMemory(this, size);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
        }

        ~VertexBuffer()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            memoryOwner.Dispose();
            memoryOwner = null;
            vertexMemory = null;

            if (isInitialised)
            {
                Unbind();

                memoryLease?.Dispose();
                GL.DeleteBuffer(vboId);
            }

            IsDisposed = true;
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (!isInitialised)
            {
                Initialise();
                isInitialised = true;
            }

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        public void Draw()
        {
            DrawRange(0, vertexMemory.Length);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            Bind(true);

            int amountVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(amountVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, vertexMemory.Length);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);

            int amountVertices = endIndex - startIndex;
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * STRIDE), (IntPtr)(amountVertices * STRIDE), ref vertexMemory.Span[startIndex]);

            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, amountVertices);
        }
    }
}
