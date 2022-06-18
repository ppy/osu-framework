// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public abstract class VertexBuffer<T> : IVertexBuffer, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        /// <summary>
        /// Gets the number of vertices in this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        public int Capacity { get; }

        public int Count { get; private set; }

        internal static readonly int STRIDE = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        internal readonly DepthWrappingVertex<T>[] Vertices;
#endif

        private readonly BufferUsageHint usage;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly GlobalStatistic<int> vertex_memory_statistic = GlobalStatistics.Get<int>("Native", $"{nameof(VertexBuffer<T>)}");

        internal int VboId { get; private set; } = -1;

        protected VertexBuffer(int capacity, BufferUsageHint usage)
        {
            this.usage = usage;

            Capacity = capacity;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            Vertices = new DepthWrappingVertex<T>[capacity];
#endif
        }

        public void Advance(int count)
        {
            ensureSpace(count);
            Count += count;
        }

        public void Push(T vertex)
        {
            ensureSpace(1);

            VertexUploadQueue<T>.Enqueue(this, Count, vertex);
            Count++;
        }

        private void ensureSpace(int requirement)
        {
            if (Count + requirement > Capacity)
                throw new InvalidOperationException("Vertex buffer has no more space left");
        }

        /// <summary>
        /// Initialises this <see cref="VertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out int id);
            VboId = id;

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, VboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Capacity * STRIDE;
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
            vertex_memory_statistic.Value += size;

            GLWrapper.RegisterVertexBufferUse(this);
        }

        ~VertexBuffer()
        {
            GLWrapper.ScheduleDisposal(v => v.Dispose(false), this);
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

            ((IVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (VboId == -1)
                Initialise();

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, VboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        /// <summary>
        /// How many times this <see cref="VertexBuffer{T}"/> has been drawn.
        /// </summary>
        public ulong DrawCount { get; private set; }

        public void Draw()
        {
            if (Count == 0)
                return;

            LastUseResetId = GLWrapper.ResetId;

            VertexUploadQueue<T>.Upload();

            Bind(true);
            GL.DrawElements(Type, ToElements(Count), DrawElementsType.UnsignedShort, IntPtr.Zero);
            Unbind();

            DrawCount++;
            Count = 0;

            LastDrawHadOverflowVertices = ThisDrawHasOverflowVertices;
            ThisDrawHasOverflowVertices = false;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        /// <summary>
        /// Whether this draw call's vertices are "overflow" vertices.
        /// </summary>
        public bool ThisDrawHasOverflowVertices { get; set; }

        /// <summary>
        /// Whether the last draw call's vertices were "overflow" vertices.
        /// </summary>
        public bool LastDrawHadOverflowVertices { get; set; }

        void IVertexBuffer.Free()
        {
            if (VboId != -1)
            {
                Unbind();

                GL.DeleteBuffer(VboId);
                VboId = -1;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                Vertices.AsSpan().Clear();
#endif

                vertex_memory_statistic.Value -= Capacity * STRIDE;
            }

            LastUseResetId = 0;
        }
    }
}
