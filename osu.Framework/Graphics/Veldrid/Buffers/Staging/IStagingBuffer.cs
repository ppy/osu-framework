// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers.Staging
{
    internal interface IStagingBuffer<T> : IDisposable
        where T : unmanaged
    {
        /// <summary>
        /// The total size of this buffer in bytes.
        /// </summary>
        uint SizeInBytes { get; }

        /// <summary>
        /// The number of elements in this buffer.
        /// </summary>
        uint Count { get; }

        /// <summary>
        /// Any extra flags required for the target of a <see cref="CopyTo"/> operation.
        /// </summary>
        BufferUsage CopyTargetUsageFlags { get; }

        /// <summary>
        /// The data contained in this buffer.
        /// </summary>
        Span<T> Data { get; }

        /// <summary>
        /// Copies data from this buffer into a <see cref="DeviceBuffer"/>.
        /// </summary>
        /// <param name="buffer">The target buffer.</param>
        /// <param name="srcOffset">The offset into this buffer at which the copy should start.</param>
        /// <param name="dstOffset">The offset into <paramref name="buffer"/> at which the copy should start.</param>
        /// <param name="size">The number of elements to be copied.</param>
        void CopyTo(DeviceBuffer buffer, uint srcOffset, uint dstOffset, uint size);
    }
}
