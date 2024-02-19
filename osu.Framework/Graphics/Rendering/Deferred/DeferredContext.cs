// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredContext
    {
        public GraphicsDevice Device
            => Renderer.Device;

        public VeldridDevice VeldridDevice
            => Renderer.VeldridDevice;

        public readonly DeferredRenderer Renderer;
        public readonly ResourceAllocator Allocator;
        public readonly EventList RenderEvents;
        public readonly UniformBufferManager UniformBufferManager;
        public readonly VertexManager VertexManager;

        public DeferredContext(DeferredRenderer renderer)
        {
            Renderer = renderer;
            Allocator = new ResourceAllocator();
            RenderEvents = new EventList(Allocator);
            UniformBufferManager = new UniformBufferManager(this);
            VertexManager = new VertexManager(this);
        }

        public void NewFrame()
        {
            Allocator.Reset();
            RenderEvents.Reset();
            UniformBufferManager.Reset();
            VertexManager.Reset();
        }

        public ResourceReference Reference<T>(T obj)
            where T : class
            => Allocator.Reference(obj);

        public object Dereference(ResourceReference reference)
            => Allocator.Dereference(reference);

        public T Dereference<T>(ResourceReference reference)
            => (T)Allocator.Dereference(reference);

        public ResourceReference NullReference()
            => Allocator.NullReference();

        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
            => Allocator.AllocateObject(data);

        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
            => Allocator.AllocateRegion(data);

        public MemoryReference AllocateRegion(int length)
            => Allocator.AllocateRegion(length);

        public Span<byte> GetRegion(MemoryReference reference)
            => Allocator.GetRegion(reference);

        public void EnqueueEvent<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
            => RenderEvents.Enqueue(renderEvent);
    }
}
