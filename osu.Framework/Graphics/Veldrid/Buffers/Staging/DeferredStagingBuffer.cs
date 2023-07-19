// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.Memory;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers.Staging
{
    /// <summary>
    /// A staging buffer that stores data in managed memory and uses an intermediate driver buffer for copies.
    /// </summary>
    internal class DeferredStagingBuffer<T> : IStagingBuffer<T>
        where T : unmanaged
    {
        private readonly VeldridRenderer renderer;
        private readonly IMemoryOwner<T> memoryOwner;

        private readonly Memory<T> cpuBuffer;
        private readonly DeviceBuffer driverBuffer;

        public DeferredStagingBuffer(VeldridRenderer renderer, uint count)
        {
            this.renderer = renderer;
            Count = count;
            SizeInBytes = (uint)(Unsafe.SizeOf<T>() * count);

            memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<T>((int)Count, AllocationOptions.Clean);
            cpuBuffer = memoryOwner.Memory;

            driverBuffer = renderer.Factory.CreateBuffer(new BufferDescription(SizeInBytes, BufferUsage.Staging));
        }

        public uint SizeInBytes { get; }

        public uint Count { get; }

        public BufferUsage CopyTargetUsageFlags => 0;

        public Span<T> Data => cpuBuffer.Span;

        public void CopyTo(DeviceBuffer buffer, uint srcOffset, uint dstOffset, uint count)
        {
            renderer.Device.UpdateBuffer(
                driverBuffer,
                (uint)(srcOffset * Unsafe.SizeOf<T>()),
                ref Data[(int)srcOffset],
                (uint)(count * Unsafe.SizeOf<T>()));

            renderer.BufferUpdateCommands.CopyBuffer(
                driverBuffer,
                (uint)(srcOffset * Unsafe.SizeOf<T>()),
                buffer,
                (uint)(dstOffset * Unsafe.SizeOf<T>()),
                (uint)(count * Unsafe.SizeOf<T>()));
        }

        public void Dispose()
        {
            memoryOwner.Dispose();
            driverBuffer.Dispose();
        }
    }
}
