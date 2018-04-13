// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches
{
    public abstract class VertexBatch<T> : IVertexBatch, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public List<VertexBuffer<T>> VertexBuffers = new List<VertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentIndex;
        private int currentVertex;
        private int lastVertex;

        private readonly int maxBuffers;

        private VertexBuffer<T> currentVertexBuffer => VertexBuffers[currentIndex];

        protected VertexBatch(int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;
            this.maxBuffers = maxBuffers;

            AddAction = Add;
        }

        #region Disposal

        ~VertexBatch()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                foreach (VertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
        }

        #endregion

        public void ResetCounters()
        {
            changeBeginIndex = -1;
            currentIndex = 0;
            currentVertex = 0;
            lastVertex = 0;
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void Add(T v)
        {
            GLWrapper.SetActiveBatch(this);

            while (currentIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());

            VertexBuffer<T> vertexBuffer = currentVertexBuffer;

            if (!vertexBuffer.Vertices[currentVertex].Equals(v))
            {
                if (changeBeginIndex == -1)
                    changeBeginIndex = currentVertex;

                changeEndIndex = currentVertex + 1;
            }

            vertexBuffer.Vertices[currentVertex] = v;
            ++currentVertex;

            if (currentVertex >= vertexBuffer.Vertices.Length)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
                lastVertex = currentVertex = 0;
            }
        }

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// This is a cached delegate of <see cref="Add"/> that should be used in memory-critical locations such as <see cref="DrawNode"/>s.
        /// </summary>
        public readonly Action<T> AddAction;

        public int Draw()
        {
            if (currentVertex == lastVertex)
                return 0;

            VertexBuffer<T> vertexBuffer = currentVertexBuffer;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(lastVertex, currentVertex);

            int count = currentVertex - lastVertex;

            // When using multiple buffers we advance to the next one with every draw to prevent contention on the same buffer with future vertex updates.
            //TODO: let us know if we exceed and roll over to zero here.
            currentIndex = (currentIndex + 1) % maxBuffers;
            currentVertex = 0;

            lastVertex = currentVertex;
            changeBeginIndex = -1;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }
    }
}
