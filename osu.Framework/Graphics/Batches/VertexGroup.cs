// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// A grouping of vertices in a <see cref="DrawNode"/>.
    /// </summary>
    /// <remarks>
    /// Ensure to store this object in the <see cref="DrawNode"/>.
    /// </remarks>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    public struct VertexGroup<TVertex> : IVertexGroup<TVertex>, IDisposable
        where TVertex : struct, IEquatable<TVertex>, IVertex
    {
        internal readonly VertexBatch<TVertex> Batch;
        internal readonly long InvalidationID;
        internal readonly int StartIndex;
        internal readonly float DrawDepth;

        internal ulong FrameIndex;
        internal bool DrawRequired;
        internal int Count;

        public VertexGroup(VertexBatch<TVertex> batch, long invalidationID, int startIndex, float drawDepth)
        {
            Batch = batch;
            InvalidationID = invalidationID;
            StartIndex = startIndex;
            DrawDepth = drawDepth;

            FrameIndex = 0;
            DrawRequired = false;
            Count = 0;
        }

        public void Add(TVertex vertex)
        {
            if (DrawRequired)
                Batch.AddVertex(vertex);
            else
            {
#if VBO_CONSISTENCY_CHECKS
                if (!Batch.GetCurrentVertex().Equals(vertex))
                    throw new InvalidOperationException("Vertex draw was skipped, but the contained vertex differs.");
#endif

                Batch.Advance();
            }

            Count++;
        }

        public void Dispose()
        {
        }
    }
}
