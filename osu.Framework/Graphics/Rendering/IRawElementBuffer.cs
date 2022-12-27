// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Graphics.Rendering
{
    /// <inheritdoc/>
    /// <typeparam name="TIndex">The type of indices this element buffer stores. This can be either <see cref="ushort"/> or <see cref="uint"/>.</typeparam>
    public interface IRawElementBuffer<TIndex> : IRawElementBuffer, IRawBuffer<TIndex> where TIndex : unmanaged, IConvertible
    {
        public static readonly IndexType INDEX_TYPE;

        static IRawElementBuffer()
        {
            if (typeof(TIndex) == typeof(ushort))
            {
                INDEX_TYPE = IndexType.UnsignedShort;
            }
            else if (typeof(TIndex) == typeof(uint))
            {
                INDEX_TYPE = IndexType.UnsignedInt;
            }
            else
            {
                throw new NotSupportedException($@"An element buffer might only contain UInt16 or UInt32 indices, but {typeof(TIndex).ReadableName()} was specified.");
            }
        }
    }

    /// <summary>
    /// A GPU buffer of indices pointing into one or more <see cref="IRawVertexBuffer"/>s.
    /// </summary>
    public interface IRawElementBuffer : IRawBuffer
    {
        /// <summary>
        /// Binds the element buffer.
        /// </summary>
        /// <remarks>
        /// This call is cached by the bound (or implicit) <see cref="IRawVertexArray"/>.
        /// </remarks>
        /// <returns>Whether the bind was necessary.</returns>
        abstract bool IRawBuffer.Bind();

        /// <summary>
        /// Unbinds the element buffer.
        /// </summary>
        /// <remarks>
        /// This call is cached by the bound (or implicit) <see cref="IRawVertexArray"/>.
        /// </remarks>
        abstract void IRawBuffer.Unbind();

        /// <summary>
        /// Draws the vertices pointed to by the indices stored in this buffer.
        /// This requires this buffer to be bound and the layout of some <see cref="IRawVertexArray"/>s to be set.
        /// </summary>
        /// <param name="topology">The topology of drawn elements.</param>
        /// <param name="count">The number of indices to draw.</param>
        /// <param name="offset">Offset (in indices) from the start of this buffer.</param>
        void Draw(PrimitiveTopology topology, int count, int offset = 0);
    }
}
