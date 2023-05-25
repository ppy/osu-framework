// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers.Staging
{
    /// <summary>
    /// A staging buffer that uses a persistently-mapped device buffer as its storage medium.
    /// </summary>
    internal class PersistentStagingBuffer<T> : IStagingBuffer<T>
        where T : unmanaged
    {
        private readonly VeldridRenderer renderer;
        private readonly DeviceBuffer stagingBuffer;
        private MappedResource? stagingBufferMap;

        public PersistentStagingBuffer(VeldridRenderer renderer, uint count)
        {
            this.renderer = renderer;

            Count = count;
            SizeInBytes = (uint)(Unsafe.SizeOf<T>() * count);

            stagingBuffer = renderer.Factory.CreateBuffer(new BufferDescription(SizeInBytes, BufferUsage.Staging));

            Data.Clear();
        }

        public uint SizeInBytes { get; }

        public uint Count { get; }

        public BufferUsage CopyTargetUsageFlags => 0;

        public Span<T> Data
        {
            get
            {
                if (stagingBufferMap is not MappedResource map)
                    stagingBufferMap = map = renderer.Device.Map(stagingBuffer, MapMode.ReadWrite);

                unsafe
                {
                    return new Span<T>(map.Data.ToPointer(), (int)Count);
                }
            }
        }

        public void CopyTo(DeviceBuffer buffer, uint srcOffset, uint dstOffset, uint size)
        {
            unmap();

            renderer.BufferUpdateCommands.CopyBuffer(
                stagingBuffer,
                (uint)(srcOffset * Unsafe.SizeOf<T>()),
                buffer,
                (uint)(dstOffset * Unsafe.SizeOf<T>()),
                (uint)(size * Unsafe.SizeOf<T>()));
        }

        private void unmap()
        {
            if (stagingBufferMap == null)
                return;

            renderer.Device.Unmap(stagingBuffer);
            stagingBufferMap = null;
        }

        public void Dispose()
        {
            unmap();
            stagingBuffer.Dispose();
        }
    }
}
