// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using OpenTK.Graphics.ES30;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public abstract class VertexBuffer<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public T[] Vertices;

        protected static readonly int STRIDE = VertexUtils<T>.STRIDE;

        private readonly int vboId;
        private readonly BufferUsageHint usage;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;
            GL.GenBuffers(1, out vboId);

            resize(amountVertices);
        }

        ~VertexBuffer()
        {
            Dispose(false);
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

            Unbind();

            GLWrapper.DeleteBuffer(vboId);

            IsDisposed = true;
        }

        private void resize(int amountVertices)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not resize disposed vertex buffers.");

            T[] oldVertices = Vertices;
            Vertices = new T[amountVertices];

            if (oldVertices != null)
                for (int i = 0; i < oldVertices.Length && i < Vertices.Length; ++i)
                    Vertices[i] = oldVertices[i];

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<T>.Bind();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * STRIDE), IntPtr.Zero, usage);
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<T>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices)
        {
            return vertices;
        }

        protected virtual int ToElementIndex(int vertexIndex)
        {
            return vertexIndex;
        }

        protected abstract PrimitiveType Type { get; }

        public void Draw()
        {
            DrawRange(0, Vertices.Length);
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
            UpdateRange(0, Vertices.Length);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);

            int amountVertices = endIndex - startIndex;
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * STRIDE), (IntPtr)(amountVertices * STRIDE), ref Vertices[startIndex]);

            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, amountVertices);
        }
    }
}
