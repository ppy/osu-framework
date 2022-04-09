// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    public struct VertexGroup<T> : IVertexGroup<T>, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        internal readonly VertexBatch<T> Batch;
        internal readonly long InvalidationID;
        internal readonly int StartIndex;
        internal readonly float DrawDepth;

        internal ulong FrameIndex;
        internal bool DrawRequired;
        internal int Count;

        public VertexGroup(VertexBatch<T> batch, long invalidationID, int startIndex, float drawDepth)
        {
            Batch = batch;
            InvalidationID = invalidationID;
            StartIndex = startIndex;
            DrawDepth = drawDepth;

            FrameIndex = 0;
            DrawRequired = false;
            Count = 0;
        }

        public void Add(T vertex)
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
