// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches.Internal
{
    /// <summary>
    /// A vertex batch.
    /// </summary>
    internal interface IVertexBatch
    {
        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="vertices">The group to add the vertex to.</param>
        /// <param name="vertex">The vertex to add.</param>
        void Add<TInput>(IVertexGroup vertices, TInput vertex)
            where TInput : struct, IEquatable<TInput>, IVertex;

        /// <summary>
        /// Advances beyond the current vertex.
        /// </summary>
        void Advance(int count);

        /// <summary>
        /// Notifies that a <see cref="VertexGroup{TVertex}"/> usage has begun.
        /// </summary>
        void UsageStarted();

        /// <summary>
        /// Notifies that the <see cref="VertexGroup{TVertex}"/> usage has finished.
        /// </summary>
        void UsageFinished();

#if DEBUG && !NO_VBO_CONSISTENCY_CHECKS
        /// <summary>
        /// Ensures that the current vertex matches a given one, and outputs a message if not.
        /// </summary>
        void EnsureCurrentVertex<T>(IVertexGroup vertices, T vertex, string failureMessage)
            where T : struct, IEquatable<T>, IVertex;
#endif

        /// <summary>
        /// Draws this <see cref="IVertexBatch"/>.
        /// </summary>
        int Draw();

        /// <summary>
        /// Resets this <see cref="IVertexBatch"/> for a new frame.
        /// </summary>
        void ResetCounters();
    }
}
