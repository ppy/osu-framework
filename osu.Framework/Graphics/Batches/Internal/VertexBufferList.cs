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
    //   - A "spill" indicates a new buffer has been moved to.
    //     This can happen in two ways:
    //       - A draw call (flush) has been triggered (e.g. shader, masking, texture changes).
    //       - A vertex buffer has been filled to its capacity.
    //     The buffer that has spilled _may not_ be drawn to again. Doing so comes at a significant loss of performance.
    // EDGE CASE:
    //   - If we wrap around to the same buffer, then we have no choice to re-use that buffer..
    public class VertexBufferList<T> : IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public event Action<VertexBuffer<T>>? OnSpill;

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        private readonly List<T[]> arrayBuffers = new List<T[]>();
#endif

        private readonly List<VertexBuffer<T>> buffers = new List<VertexBuffer<T>>();
        private readonly Func<VertexBuffer<T>> createBufferFunc;
        private readonly int maxBuffers;

        public ulong CurrentBufferDrawCount => !hasSpace() ? 0 : getCurrentBuffer().DrawCount;

        public int CurrentBufferIndex { get; private set; }
        public int CurrentVertexIndex => !hasSpace() ? 0 : getCurrentBuffer().Count;

        public VertexBufferList(int maxBuffers, Func<VertexBuffer<T>> createBufferFunc)
        {
            if (maxBuffers <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBuffers), maxBuffers, "Maximum number of vertex buffers must be greater than 0.");

            this.maxBuffers = maxBuffers;
            this.createBufferFunc = createBufferFunc;
        }

        public void Reset()
        {
            // If the current vertex buffer has any vertices remaining, spill it.
            if (hasSpace() && getCurrentBuffer().Count > 0)
                Spill();

            CurrentBufferIndex = 0;
        }

        public void Push()
        {
            ensureHasSpace();
            getCurrentBuffer().Push();
            checkForSpill();
        }

        public void Push(T vertex)
        {
            ensureHasSpace();

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            arrayBuffers[CurrentBufferIndex][CurrentVertexIndex] = vertex;
            AssertIsCurrentVertex(vertex, "Added vertex does not equal the given one. Vertex equality comparer is probably broken.");
#endif

            getCurrentBuffer().Push(vertex);
            checkForSpill();
        }

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        internal void AssertIsCurrentVertex(T vertex, string failureMessage)
        {
            if (!arrayBuffers[CurrentBufferIndex][CurrentVertexIndex].Equals(vertex))
                throw new InvalidOperationException(failureMessage);
        }
#endif

        public void Spill()
        {
            if (!hasSpace())
                return;

            if (getCurrentBuffer().Count == 0)
                return;

            OnSpill?.Invoke(getCurrentBuffer());
            CurrentBufferIndex++;

            // Wrap back to 0 if we can't fit any more buffers.
            if (CurrentBufferIndex == maxBuffers)
            {
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
                CurrentBufferIndex = 0;
            }
        }

        private void ensureHasSpace()
        {
            if (!hasSpace())
            {
                buffers.Add(createBufferFunc());

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
                arrayBuffers.Add(new T[buffers[^1].Capacity]);
#endif
            }
        }

        private void checkForSpill()
        {
            Debug.Assert(hasSpace());

            if (getCurrentBuffer().Count == getCurrentBuffer().Capacity)
                Spill();
        }

        private VertexBuffer<T> getCurrentBuffer() => buffers[CurrentBufferIndex];
        private bool hasSpace() => CurrentBufferIndex < buffers.Count;

        public void Dispose()
        {
            foreach (VertexBuffer<T> vbo in buffers)
                vbo.Dispose();
            buffers.Clear();

            CurrentBufferIndex = 0;
        }
    }
}
