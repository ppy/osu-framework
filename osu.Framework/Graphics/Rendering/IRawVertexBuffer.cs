// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering
{
    /// <inheritdoc/>
    /// <typeparam name="TVertex">The type of vertices this vertex buffer stores.</typeparam>
    public interface IRawVertexBuffer<TVertex> : IRawVertexBuffer, IRawBuffer<TVertex> where TVertex : unmanaged, IVertex
    {
        /// <summary>
        /// Sets the layout of this vertex buffer when drawing using the explicit layout of the <see cref="TVertex"/>.
        /// This requires the buffer to be bound.
        /// </summary>
        /// <remarks>
        /// This call is cached by the bound (or implicit) <see cref="IRawVertexArray"/>.
        /// </remarks>
        void SetLayout();
    }

    /// <summary>
    /// A GPU buffer of per-vertex data (such as position, uv coordinates or colour).
    /// </summary>
    public interface IRawVertexBuffer : IRawBuffer
    {
        /// <summary>
        /// Sets the layout of this vertex buffer when drawing.
        /// This requires the buffer to be bound.
        /// </summary>
        /// <param name="layoutPositions">The layout positions of individual vertex components.</param>
        /// <remarks>
        /// This call is cached by the bound (or implicit) <see cref="IRawVertexArray"/>.
        /// </remarks>
        void SetLayout(ReadOnlySpan<int> layoutPositions);

        /// <summary>
        /// Draws the vertices stored in this buffer.
        /// This requires the layout of this vertex buffer to be set.
        /// </summary>
        /// <param name="topology">The topology of drawn elements.</param>
        /// <param name="count">The number of vertices to draw.</param>
        /// <param name="offset">Offset (in vertices) from the start of this buffer.</param>
        void Draw(PrimitiveTopology topology, int count, int offset = 0);
    }
}
