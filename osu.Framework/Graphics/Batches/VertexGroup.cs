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
    public struct VertexGroup<TVertex> : IVertexGroup<TVertex>
        where TVertex : struct, IEquatable<TVertex>, IVertex
    {
        /// <summary>
        /// The <see cref="VertexBatch{T}"/> where vertices are to be added.
        /// </summary>
        internal VertexBatch<TVertex> Batch;

        /// <summary>
        /// The <see cref="DrawNode"/> invalidation ID when this <see cref="VertexGroup{TVertex}"/> was last used.
        /// </summary>
        internal long InvalidationID;

        /// <summary>
        /// The index inside the <see cref="VertexBatch{T}"/> where this <see cref="VertexGroup{TVertex}"/> last had its vertices placed.
        /// </summary>
        internal int StartIndex;

        /// <summary>
        /// The <see cref="DrawNode"/> draw depth when this <see cref="VertexGroup{TVertex}"/> was last used.
        /// </summary>
        internal float DrawDepth;

        /// <summary>
        /// The draw frame when this <see cref="VertexGroup{TVertex}"/> was last used.
        /// </summary>
        internal ulong FrameIndex;

        /// <summary>
        /// Whether this <see cref="VertexGroup{TVertex}"/> needs to upload vertices to the <see cref="Batch"/>.
        /// If <c>false</c>, uploads will be omitted either automatically via <see cref="Add"/> or more aggressively via manual calls to <see cref="TrySkip"/>.
        /// </summary>
        internal bool UploadRequired;

        public void Add(TVertex vertex) => Batch.AddVertex(ref this, vertex);

        public bool TrySkip(int count)
        {
#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
            return false;
#else
            if (UploadRequired)
                return false;

            Batch.Advance(count);
            return true;
#endif
        }
    }
}
