// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridVertexBuffer<in T> : IVertexBuffer, IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = VeldridVertexUtils<DepthWrappingVertex<T>>.STRIDE;

        public static readonly VertexLayoutDescription LAYOUT = VeldridVertexUtils<DepthWrappingVertex<T>>.Layout;

        /// <summary>
        /// Gets the number of vertices in this <see cref="IVeldridVertexBuffer{T}"/>.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The underlying <see cref="DeviceBuffer"/> to bind for drawing.
        /// </summary>
        DeviceBuffer Buffer { get; }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VeldridVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether synchronisation for this vertex is required. If true, <see cref="UpdateRange"/> should be called with this vertex included.</returns>
        bool SetVertex(int vertexIndex, T vertex);

        /// <summary>
        /// Populates the vertices at the specified range to the GPU buffer.
        /// </summary>
        /// <param name="from">The beginning vertex index.</param>
        /// <param name="to">The ending vertex index.</param>
        void UpdateRange(int from, int to);
    }
}
