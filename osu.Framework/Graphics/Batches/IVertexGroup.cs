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
        void Add(T vertex);
    }
}
