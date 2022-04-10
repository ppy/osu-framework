// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        protected static readonly int STRIDE = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        internal readonly DepthWrappingVertex<T>[] Vertices;
#endif

        private readonly BufferUsageHint usage;
        private static readonly DepthWrappingVertex<T>[] upload_queue = new DepthWrappingVertex<T>[1024];

        // ReSharper disable once StaticMemberInGenericType
        private static int uploadStart = int.MaxValue;

        // ReSharper disable once StaticMemberInGenericType
        private static int uploadLength;

        internal int VboId { get; private set; } = -1;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;

            Size = amountVertices;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            Vertices = new DepthWrappingVertex<T>[amountVertices];
#endif
        }

        public void EnqueueVertex(int index, T vertex)
        {
            // A new upload must be started if the queue can't hold any more vertices, or if the enqueued index is disjoint from the current to-be-uploaded set.
            if (uploadLength == upload_queue.Length || uploadLength > 0 && index > uploadStart + uploadLength)
                upload();

            uploadStart = Math.Min(uploadStart, index);
            upload_queue[uploadLength++] = new DepthWrappingVertex<T>
            {
                Vertex = vertex,
                BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth
            };

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            Vertices[index] = upload_queue[uploadLength - 1];
#endif
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

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

            int size = Size * STRIDE;

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);

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

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            LastUseResetId = GLWrapper.ResetId;

            upload();

            Bind(true);

            int countVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(countVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));

            Unbind();
        }

        private void upload()
        {
            if (uploadLength == 0)
                return;

            Bind(false);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(uploadStart * STRIDE), (IntPtr)(uploadLength * STRIDE), ref upload_queue[0]);
            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, uploadLength);

            uploadStart = int.MaxValue;
            uploadLength = 0;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (VboId != -1)
            {
                Unbind();

                GL.DeleteBuffer(VboId);
                VboId = -1;
            }

            LastUseResetId = 0;
        }
    }
}
