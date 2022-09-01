// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Buffers;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering.Vertices;
using SixLabors.ImageSharp.Memory;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal abstract class GLVertexBuffer<T> : IGLVertexBuffer, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        /// <summary>
        /// The maximum number of vertices supported by this buffer.
        /// </summary>
        public const int MAX_VERTICES = ushort.MaxValue;

        protected static readonly int STRIDE = GLVertexUtils<DepthWrappingVertex<T>>.STRIDE;

        protected readonly GLRenderer Renderer;
        private readonly BufferUsageHint usage;

        private Memory<DepthWrappingVertex<T>> vertexMemory;
        private IMemoryOwner<DepthWrappingVertex<T>> memoryOwner;

        private int vboId = -1;

        protected GLVertexBuffer(GLRenderer renderer, int amountVertices, BufferUsageHint usage)
        {
            Renderer = renderer;
            this.usage = usage;

            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref getMemory().Span[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex) || currentVertex.BackbufferDrawDepth != Renderer.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = Renderer.BackbufferDrawDepth;

            return isNewVertex;
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Initialises this <see cref="GLVertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out vboId);

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
        }

        ~GLVertexBuffer()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            ((IGLVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (vboId == -1)
                Initialise();

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            Bind(true);

            int countVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(countVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);

            int countVertices = endIndex - startIndex;
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * STRIDE), (IntPtr)(countVertices * STRIDE), ref getMemory().Span[startIndex]);

            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        private ref Memory<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(Size, AllocationOptions.Clean);
                vertexMemory = memoryOwner.Memory;

                Renderer.RegisterVertexBufferUse(this);
            }

            LastUseResetId = Renderer.ResetId;

            return ref vertexMemory;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IGLVertexBuffer.Free()
        {
            if (vboId != -1)
            {
                Unbind();

                GL.DeleteBuffer(vboId);
                vboId = -1;
            }

            memoryOwner?.Dispose();
            memoryOwner = null;
            vertexMemory = Memory<DepthWrappingVertex<T>>.Empty;

            LastUseResetId = 0;
        }
    }
}
