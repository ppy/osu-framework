// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Veldrid.Buffers;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredUniformBuffer : IVeldridUniformBuffer
    {
        /// <summary>
        /// Writes data to the buffer.
        /// </summary>
        /// <param name="memory">The data to write.</param>
        /// <returns>A reference to the written data.</returns>
        UniformBufferReference Write(in MemoryReference memory);

        /// <summary>
        /// Activates the given uniform buffer in the graphics pipeline.
        /// </summary>
        /// <param name="chunk">The uniform buffer, represented as a chunk of the full buffer.</param>
        void Activate(UniformBufferChunk chunk);
    }
}
