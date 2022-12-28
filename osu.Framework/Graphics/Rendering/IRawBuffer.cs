// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <inheritdoc/>
    /// <typeparam name="TData">The type of data this buffer stores.</typeparam>
    public interface IRawBuffer<TData> : IRawBuffer where TData : unmanaged
    {
        /// <summary>
        /// Reallocates and clears the data store so that is has excatly the given size.
        /// This requires the buffer to be bound.
        /// </summary>
        /// <remarks>
        /// Use <see cref="UpdateRange(ReadOnlySpan{TData}, int)"/> for subsequent calls if a resize is not necessary.
        /// </remarks>
        /// <param name="size">The capacity.</param>
        /// <param name="usageHint">A usage hint.</param>
        void SetCapacity(int size, BufferUsageHint usageHint);

        /// <summary>
        /// Sends data to the GPU, reallocating the data store.
        /// This requires the buffer to be bound.
        /// </summary>
        /// <remarks>
        /// This call reallocates the data store.
        /// Use <see cref="UpdateRange(ReadOnlySpan{TData}, int)"/> for subsequent calls if a resize is not necessary.
        /// </remarks>
        /// <param name="data">The data to upload.</param>
        /// <param name="usageHint">A usage hint.</param>
        void BufferData(ReadOnlySpan<TData> data, BufferUsageHint usageHint);

        /// <summary>
        /// Updates a range of data.
        /// This requires the buffer to be bound.
        /// </summary>
        /// <param name="data">The data to upload.</param>
        /// <param name="offset">Offset from the start of the buffer (not the uploaded data).</param>
        void UpdateRange(ReadOnlySpan<TData> data, int offset = 0);
    }

    /// <summary>
    /// A GPU buffer of arbitrary data.
    /// </summary>
    public interface IRawBuffer : IDisposable
    {
        /// <summary>
        /// Binds the buffer.
        /// </summary>
        /// <returns>Whether the bind was necessary.</returns>
        bool Bind();

        /// <summary>
        /// Unbinds the buffer.
        /// </summary>
        void Unbind();
    }
}
