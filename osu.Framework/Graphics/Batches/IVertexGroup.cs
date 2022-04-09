// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Batches
{
    /// <summary>
    /// An interface for a grouping of vertices.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    public interface IVertexGroup<in T>
        where T : struct, IEquatable<T>, IVertex
    {
        /// <summary>
        /// Adds a vertex to this group.
        /// </summary>
        /// <param name="vertex">The vertex to add.</param>
        void Add(T vertex);

        /// <summary>
        /// Attempts to skip a number of vertices from the batch.
        /// </summary>
        /// <remarks>
        /// If this returns <c>true</c>, any calls to <see cref="Add"/> must be omitted for the relevant vertices.
        /// <br />
        /// IMPORTANT: Other states such as active textures and shaders must be brought into the correct states regardless of return value.
        /// </remarks>
        /// <param name="count">The number of vertices to skip.</param>
        /// <returns>Whether the vertices were skipped.</returns>
        bool TrySkip(int count);
    }
}
