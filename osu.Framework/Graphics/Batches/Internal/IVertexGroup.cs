// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches.Internal
{
    /// <summary>
    /// Interface for a grouping of vertices.
    /// </summary>
    internal interface IVertexGroup
    {
        /// <summary>
        /// Transforms a given vertex according to this group.
        /// </summary>
        /// <param name="vertex">The vertex to transform.</param>
        /// <typeparam name="TInput">The input vertex type.</typeparam>
        /// <typeparam name="TOutput">The output vertex type.</typeparam>
        TOutput Transform<TInput, TOutput>(TInput vertex)
            where TOutput : struct, IEquatable<TOutput>, IVertex
            where TInput : struct, IEquatable<TInput>, IVertex;

        DrawNode? DrawNode { get; set; }

        /// <summary>
        /// The <see cref="VertexBatch{T}"/> to which this group's vertices were last added.
        /// </summary>
        IVertexBatch? Batch { get; set; }

        /// <summary>
        /// The <see cref="DrawNode"/> invalidation ID when this group was last used.
        /// </summary>
        long InvalidationID { get; set; }

        /// <summary>
        /// The index of the <see cref="IVertexBuffer"/> in which this group's vertices were last placed.
        /// </summary>
        int BufferIndex { get; set; }

        /// <summary>
        /// The index into the <see cref="IVertexBuffer"/> at which this group's vertices were placed.
        /// </summary>
        int VertexIndex { get; set; }

        /// <summary>
        /// The <see cref="DrawNode"/> draw depth when this group was last used.
        /// </summary>
        float DrawDepth { get; set; }

        /// <summary>
        /// The draw frame when this group was last used.
        /// </summary>
        ulong FrameIndex { get; set; }

        /// <summary>
        /// Whether this <see cref="IVertexGroup"/> was one that caused a VBO overflow.
        /// </summary>
        bool TriggeredOverflow { get; set; }

        /// <summary>
        /// Whether vertices need to be uploaded for the batch.
        /// If <c>false</c>, uploads will be omitted either automatically via <see cref="VertexGroupUsage{T}.Add"/> or more aggressively via manual calls to <see cref="VertexGroupUsage{T}.TrySkip"/>.
        /// </summary>
        bool UploadRequired { get; set; }
    }
}
