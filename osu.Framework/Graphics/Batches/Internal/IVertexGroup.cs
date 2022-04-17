// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
    }
}
