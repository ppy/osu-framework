// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches.Internal
{
    // Broad requirements of this class:
    //   - A "commit" indicates a buffer is ready to be drawn.
    //     This can happen in two ways:
    //       - A draw call (flush) was triggered (e.g. shader, masking, texture changes).
    //       - The vertex buffer was filled up.
    //     The committed buffer should not be drawn to again, otherwise significant loss of performance will be incurred.
    //   - An "overflow" indicates that all available buffers have been exhausted.
    //     Drawing may proceed from the 0th buffer (at a significant performance loss).
    //     Any buffer that is overflowed into (has vertices written to it after an overflow), needs to be marked as invalid for the next frame.
    internal class VertexBufferList<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public event Action<VertexBuffer<T>>? OnCommit;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        private readonly List<T[]> arrayBuffers = new List<T[]>();
#endif

        private readonly List<VertexBuffer<T>> buffers = new List<VertexBuffer<T>>();
        private readonly Func<VertexBuffer<T>> createBufferFunc;
        private readonly int maxBuffers;

        /// <summary>
        /// The current vertex buffer index.
        /// </summary>
        public int CurrentBufferIndex { get; private set; }

        /// <summary>
        /// The vertex index inside the current vertex buffer.
        /// </summary>
        public int CurrentVertexIndex => !hasCurrentBuffer() ? 0 : getCurrentBuffer().Count;

        /// <summary>
        /// Whether the current vertex buffer's last draw call contained "overflow" vertices.
        /// </summary>
        public bool LastDrawHadOverflowVertices => hasCurrentBuffer() && getCurrentBuffer().LastDrawHadOverflowVertices;

        /// <summary>
        /// Whether the current draw has "overflow" vertices.
        /// </summary>
        public bool ThisDrawHasOverflowVertices { get; private set; }

        /// <summary>
        /// Creates a new <see cref="VertexBufferList{T}"/>.
        /// </summary>
        /// <param name="maxBuffers">The maximum number of vertex buffers.</param>
        /// <param name="createBufferFunc">A function that creates a new <see cref="VertexBuffer{T}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <see cref="maxBuffers"/> is less than or equal to 0.</exception>
        public VertexBufferList(int maxBuffers, Func<VertexBuffer<T>> createBufferFunc)
        {
            if (maxBuffers <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBuffers), maxBuffers, "Maximum number of vertex buffers must be greater than 0.");

            this.maxBuffers = maxBuffers;
            this.createBufferFunc = createBufferFunc;
        }

        /// <summary>
        /// Resets this list for a new frame.
        /// </summary>
        public void Reset()
        {
            // If the current vertex buffer has any vertices remaining, spill it.
            if (hasCurrentBuffer() && getCurrentBuffer().Count > 0)
                Commit();

            CurrentBufferIndex = 0;
            ThisDrawHasOverflowVertices = false;
        }

        /// <summary>
        /// Advances a number of vertices from the current point.
        /// </summary>
        /// <param name="count">The number of vertices to advance by.</param>
        public void Advance(int count)
        {
            Debug.Assert(hasCurrentBuffer());

            getCurrentBuffer().Advance(count);
            commitIfFull();
        }

        /// <summary>
        /// Pushes a new vertex to this list.
        /// </summary>
        /// <param name="vertex">The vertex to push.</param>
        public void Push(T vertex)
        {
            // Check if we need to overflow back to the start.
            if (CurrentBufferIndex == maxBuffers)
            {
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
                CurrentBufferIndex = 0;
                ThisDrawHasOverflowVertices = true;
            }

            // Check if we need additional buffers.
            if (!hasCurrentBuffer())
            {
                buffers.Add(createBufferFunc());

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                arrayBuffers.Add(new T[buffers[^1].Capacity]);
#endif
            }

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            arrayBuffers[CurrentBufferIndex][CurrentVertexIndex] = vertex;
            AssertIsCurrentVertex(vertex, "Added vertex does not equal the given one. Vertex equality comparer is probably broken.");
#endif

            getCurrentBuffer().ThisDrawHasOverflowVertices = ThisDrawHasOverflowVertices;
            getCurrentBuffer().Push(vertex);

            commitIfFull();
        }

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        internal void AssertIsCurrentVertex(T vertex, string failureMessage)
        {
            if (!arrayBuffers[CurrentBufferIndex][CurrentVertexIndex].Equals(vertex))
                throw new InvalidOperationException(failureMessage);
        }
#endif

        /// <summary>
        /// Commits the current vertex buffer, if any vertices are to be drawn. Upon successful commit, a new vertex buffer is made current.
        /// </summary>
        public void Commit()
        {
            if (!hasCurrentBuffer())
                return;

            if (getCurrentBuffer().Count == 0)
                return;

            OnCommit?.Invoke(getCurrentBuffer());
            CurrentBufferIndex++;
        }

        /// <summary>
        /// Performs a <see cref="Commit"/> if the current vertex buffer is full.
        /// </summary>
        private void commitIfFull()
        {
            Debug.Assert(hasCurrentBuffer());

            if (getCurrentBuffer().Count == getCurrentBuffer().Capacity)
                Commit();
        }

        /// <summary>
        /// Retrieves the current vertex buffer.
        /// </summary>
        private VertexBuffer<T> getCurrentBuffer() => buffers[CurrentBufferIndex];

        /// <summary>
        /// Ensures that there
        /// </summary>
        /// <returns></returns>
        private bool hasCurrentBuffer() => CurrentBufferIndex < buffers.Count;

        public void Dispose()
        {
            foreach (VertexBuffer<T> vbo in buffers)
                vbo.Dispose();
            buffers.Clear();

            CurrentBufferIndex = 0;
        }
    }
}
