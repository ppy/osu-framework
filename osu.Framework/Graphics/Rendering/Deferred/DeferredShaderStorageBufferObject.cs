// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredShaderStorageBufferObject<TData> : IShaderStorageBufferObject<TData>, IDeferredShaderStorageBufferObject, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public int Size { get; }

        private readonly TData[] data;
        private readonly DeviceBuffer buffer;
        private readonly DeferredRenderer renderer;
        private readonly int elementSize;
        private readonly Dictionary<ResourceLayout, ResourceSet> resourceSets = new Dictionary<ResourceLayout, ResourceSet>();

        public DeferredShaderStorageBufferObject(DeferredRenderer renderer, int ssboSize)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            this.renderer = renderer;

            elementSize = Unsafe.SizeOf<TData>();

            Size = ssboSize;
            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, (uint)elementSize, true));

            data = new TData[Size];
        }

        public TData this[int index]
        {
            get => data[index];
            set
            {
                if (value.Equals(data[index]))
                    return;

                data[index] = value;
                renderer.Context.EnqueueEvent(SetShaderStorageBufferObjectDataEvent.Create(renderer, this, index, value));
            }
        }

        public void Write(int index, MemoryReference memory)
            => memory.WriteTo(renderer.Context, buffer, index * elementSize);

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            if (resourceSets.TryGetValue(layout, out ResourceSet? existing))
                return existing;

            VeldridInstrumentation.RecordResourceSetCreated(VeldridResourceSetKind.ShaderStorage);
            return resourceSets[layout] = renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));
        }

        public void ResetCounters()
        {
        }

        public void Dispose()
        {
            foreach ((_, ResourceSet resourceSet) in resourceSets)
                resourceSet.Dispose();

            buffer.Dispose();
        }
    }
}
