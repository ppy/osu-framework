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

        private static readonly DepthWrappingVertex<T>[] upload_queue = new DepthWrappingVertex<T>[1024];
        private static int upload_start = int.MaxValue;
        private static int upload_length;

        private readonly BufferUsageHint usage;

        private int vboId = -1;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;

            Size = amountVertices;
        }

        public void EnqueueVertex(int index, T vertex)
        {
            if (upload_length == upload_queue.Length)
                upload();

            upload_start = Math.Min(upload_start, index);
            upload_queue[upload_length++] = new DepthWrappingVertex<T>
            {
                Vertex = vertex,
                BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth
            };
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

            GL.GenBuffers(1, out vboId);

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
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

            if (vboId == -1)
                Initialise();

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
            if (upload_length == 0)
                return;

            Bind(false);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(upload_start * STRIDE), (IntPtr)(upload_length * STRIDE), ref upload_queue[0]);
            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, upload_length);

            upload_start = int.MaxValue;
            upload_length = 0;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (vboId != -1)
            {
                Unbind();

                GL.DeleteBuffer(vboId);
                vboId = -1;
            }

            LastUseResetId = 0;
        }
    }
}
