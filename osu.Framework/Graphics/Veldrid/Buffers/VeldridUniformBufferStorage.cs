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
        private readonly DeviceBuffer buffer;
        private readonly NativeMemoryTracker.NativeMemoryLease memoryLease;

        private ResourceSet? set;
        private TData data;

        private readonly IStagingBuffer<TData>? stagingBuffer;

        public VeldridUniformBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            // These pathways are faster on respective platforms.
            // Test using TestSceneVertexUploadPerformance.
            if (renderer.Device.BackendType == GraphicsBackend.Metal)
            {
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
                stagingBuffer = renderer.CreateStagingBuffer<TData>(1);
            }
            else
            {
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer));
            }

            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);
        }

        public TData Data
        {
            get => stagingBuffer?.Data[0] ?? data;
            set
            {
                data = value;

                if (stagingBuffer != null)
                {
                    stagingBuffer.Data[0] = value;
                    stagingBuffer.CopyTo(buffer, 0, 0, 1);
                }
                else
                {
                    renderer.BufferUpdateCommands.UpdateBuffer(buffer, 0, ref data);
                }
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

        public void Dispose()
        {
            buffer.Dispose();
            memoryLease.Dispose();
            set?.Dispose();
        }
    }
}
