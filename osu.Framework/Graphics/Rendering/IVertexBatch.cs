// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering
{
    public interface IVertexBatch : IDisposable
    {
        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        int Size { get; }

        int Draw();

        internal void ResetCounters();
    }

    public interface IVertexBatch<in TVertex> : IVertexBatch
        where TVertex : struct, IEquatable<TVertex>, IVertex
    {
        /// <summary>
        /// Adds a vertex to this <see cref="IVertexBatch{T}"/>.
        /// This is a cached delegate of <see cref="Add"/> that should be used in memory-critical locations such as <see cref="DrawNode"/>s.
        /// </summary>
        Action<TVertex> AddAction { get; }

        void Add(TVertex vertex);
    }
}
