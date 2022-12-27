// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Stores <see cref="IRawVertexBuffer"/> layouts and the optional associated <see cref="IRawElementBuffer"/>.
    /// </summary>
    public interface IRawVertexArray : IDisposable
    {
        /// <summary>
        /// Binds the vertex array, restoring the state cached by it.
        /// </summary>
        /// <remarks>
        /// While a vertex array is bound:
        /// <list type="number">
        /// <item>All <see cref="IRawVertexBuffer{T}.SetLayout()"/> calls will be cached by this vertex array.</item>
        /// <item>
        /// The first <see cref="IRawElementBuffer.Bind"/> call will be cached by this vertex array, and any subsequent call will replace it.
        /// An <see cref="IRawElementBuffer.Unbind"/> call will clear this cache.
        /// </item>
        /// </list>
        /// Note that an implicit vertex array exists while no other vertex array is bound.
        /// </remarks>
        /// <returns>Whether the bind was necessary.</returns>
        bool Bind();

        /// <summary>
        /// Unbinds this vertex array, restoring the state cached by the implicit vertex array.
        /// </summary>
        void Unbind();
    }
}
