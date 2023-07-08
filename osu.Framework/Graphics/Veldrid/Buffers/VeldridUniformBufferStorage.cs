// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridUniformBufferStorage<TData>
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly VeldridRenderer renderer;
        private readonly NativeMemoryTracker.NativeMemoryLease memoryLease;

        private readonly IStagingBuffer<TData> stagingBuffer;
        private readonly DeviceBuffer gpuBuffer;

        private ResourceSet? set;

        public VeldridUniformBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            stagingBuffer = renderer.CreateStagingBuffer<TData>(1);
            gpuBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer | stagingBuffer.CopyTargetUsageFlags));
            memoryLease = NativeMemoryTracker.AddMemory(this, gpuBuffer.SizeInBytes);
        }

        public TData Data
        {
            get => stagingBuffer.Data[0];
            set
            {
                stagingBuffer.Data[0] = value;
                stagingBuffer.CopyTo(gpuBuffer, 0, 0, 1);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, gpuBuffer));

        public void Dispose()
        {
            gpuBuffer.Dispose();
            memoryLease.Dispose();
            set?.Dispose();
        }
    }
}
