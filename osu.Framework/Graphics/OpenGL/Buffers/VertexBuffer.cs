// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Reflection;
using OpenTK.Graphics.ES30;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public abstract class VertexBuffer<T> : IDisposable where T : struct, IEquatable<T>
    {
        public T[] Vertices;

        private int vboId;
        private BufferUsageHint usage;

        /// <summary>
        /// The stride of the vertex type T. We use reflection since we don't want to abuse a dummy T instance combined with virtual dispatch.
        /// </summary>
        private static readonly int stride = (int)typeof(T).GetField("Stride", BindingFlags.Public | BindingFlags.Static).GetValue(null);

        /// <summary>
        /// The static Bind method of vertex type T, used to bind the correct vertex attribute locations for use in shaders.
        /// We use reflection since we don't want to abuse a dummy T instance combined with virtual dispatch.
        /// </summary>
        private static readonly Action bindAttributes =
            (Action)Delegate.CreateDelegate(
                typeof(Action),
                null,
                typeof(T).GetMethod("Bind", BindingFlags.Public | BindingFlags.Static)
            );

        public VertexBuffer(int amountVertices, BufferUsageHint usage)
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

        protected bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            Unbind();

            GLWrapper.DeleteBuffer(vboId);

            isDisposed = true;
        }

        private void resize(int amountVertices)
        {
            Debug.Assert(!isDisposed);

            T[] oldVertices = Vertices;
            Vertices = new T[amountVertices];

            if (oldVertices != null)
                for (int i = 0; i < oldVertices.Length && i < Vertices.Length; ++i)
                    Vertices[i] = oldVertices[i];

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                bindAttributes();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * stride), IntPtr.Zero, usage);
        }

        public virtual void Bind(bool forRendering)
        {
            Debug.Assert(!isDisposed);

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                bindAttributes();
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
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * stride), (IntPtr)(amountVertices * stride), ref Vertices[startIndex]);

            Unbind();

            FrameStatistics.Increment(StatisticsCounterType.VerticesUpl, amountVertices);
        }
    }
}
