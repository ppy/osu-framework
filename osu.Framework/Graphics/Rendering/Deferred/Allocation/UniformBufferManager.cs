// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Utils;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class UniformBufferManager
    {
        /// <summary>
        /// For renderers which support binding buffer ranges, the buffer is split and bound in 64KiB chunks.
        /// This is a sane value which is supported by all renderers:
        /// - D3D11: https://learn.microsoft.com/en-us/windows/win32/api/d3d11_1/nf-d3d11_1-id3d11devicecontext1-vssetconstantbuffers1
        ///   Supports 4096 constants, where a "constant" is a float4.
        /// - Metal: https://developer.apple.com/metal/Metal-Feature-Set-Tables.pdf
        ///   Supports 65536 constants, where a "constant" is a type. We'll assume float.
        /// - OpenGL: Does not explicitly define a limit.
        ///   The minimum block size defined by the standard is 16KiB, but this is based on the shader's definition rather than bind size.
        /// </summary>
        private const int buffer_chunk_size = 65536;

        /// <summary>
        /// The size of a single buffer, if the renderer supports binding buffer ranges.
        /// </summary>
        private const int max_buffer_size = 1024 * 1024;

        private readonly DeferredContext context;
        private readonly DeferredBufferPool uniformBufferPool;
        private readonly List<PooledBuffer> inUseBuffers = new List<PooledBuffer>();
        private readonly List<MappedResource> mappedBuffers = new List<MappedResource>();

        /// <summary>
        /// The maximum size of a single buffer.
        /// </summary>
        private readonly int bufferSize;

        private int currentBuffer;
        private int currentWriteIndex;

        public UniformBufferManager(DeferredContext context)
        {
            this.context = context;

            bufferSize = context.Device.Features.BufferRangeBinding ? max_buffer_size : buffer_chunk_size;
            uniformBufferPool = new DeferredBufferPool(context.VeldridDevice, (uint)bufferSize, BufferUsage.UniformBuffer, nameof(UniformBufferManager));
        }

        public UniformBufferReference Write(in MemoryReference memory)
        {
            if (currentWriteIndex + memory.Length > bufferSize)
            {
                currentBuffer++;
                currentWriteIndex = 0;
            }

            if (currentBuffer == inUseBuffers.Count)
            {
                PooledBuffer newBuffer = uniformBufferPool.Get();

                inUseBuffers.Add(newBuffer);
                mappedBuffers.Add(context.Device.Map(newBuffer.Buffer, MapMode.Write));
            }

            memory.WriteTo(context, mappedBuffers[currentBuffer], currentWriteIndex);

            int alignment = (int)context.Device.UniformBufferMinOffsetAlignment;
            int alignedLength = MathUtils.DivideRoundUp(memory.Length, alignment) * alignment;

            int writeIndex = currentWriteIndex;
            currentWriteIndex += alignedLength;

            if (context.Device.Features.BufferRangeBinding)
            {
                return new UniformBufferReference(
                    new UniformBufferChunk(
                        context.Reference(inUseBuffers[currentBuffer].Buffer),
                        writeIndex / buffer_chunk_size * buffer_chunk_size,
                        Math.Min(buffer_chunk_size, bufferSize - writeIndex)),
                    writeIndex % buffer_chunk_size);
            }

            return new UniformBufferReference(
                new UniformBufferChunk(
                    context.Reference(inUseBuffers[currentBuffer].Buffer),
                    0,
                    bufferSize),
                writeIndex);
        }

        public void Commit()
        {
            foreach (var b in mappedBuffers)
                context.Device.Unmap(b.Resource);

            mappedBuffers.Clear();
        }

        public void Reset()
        {
            uniformBufferPool.NewFrame();
            inUseBuffers.Clear();

            currentBuffer = 0;
            currentWriteIndex = 0;

            Debug.Assert(mappedBuffers.Count == 0);
        }
    }

    internal readonly record struct UniformBufferChunk(ResourceReference Buffer, int Offset, int Size);

    internal readonly record struct UniformBufferReference(UniformBufferChunk Chunk, int OffsetInChunk);
}
