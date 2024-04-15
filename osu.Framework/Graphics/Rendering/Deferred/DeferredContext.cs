// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Pipelines;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredContext
    {
        public GraphicsDevice Device
            => Renderer.Device;

        public GraphicsPipeline Graphics
            => Renderer.Graphics;

        public readonly List<RenderEvent> RenderEvents = new List<RenderEvent>();

        public readonly DeferredRenderer Renderer;
        public readonly ResourceAllocator Allocator;
        public readonly UniformBufferManager UniformBufferManager;
        public readonly VertexManager VertexManager;

        public DeferredContext(DeferredRenderer renderer)
        {
            Renderer = renderer;
            Allocator = new ResourceAllocator();
            UniformBufferManager = new UniformBufferManager(this);
            VertexManager = new VertexManager(this);
        }

        public void NewFrame()
        {
            RenderEvents.Clear();
            Allocator.NewFrame();
            UniformBufferManager.NewFrame();
            VertexManager.Reset();
        }

        /// <summary>
        /// References an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the object. May be dereferenced via <see cref="Dereference{T}"/>.</returns>
        public ResourceReference Reference<T>(T obj)
            where T : class?
            => Allocator.Reference(obj);

        /// <summary>
        /// Dereferences an object.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The object.</returns>
        public T Dereference<T>(ResourceReference reference)
            where T : class?
            => Allocator.Dereference<T>(reference);

        /// <summary>
        /// Allocates a region of memory containing an object.
        /// </summary>
        /// <param name="data">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the memory region containing the object.</returns>
        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
            => Allocator.AllocateObject(data);

        /// <summary>
        /// Allocates a region of memory containing some data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The data type.</typeparam>
        /// <returns>A reference to the memory region containing the data.</returns>
        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
            => Allocator.AllocateRegion(data);

        /// <summary>
        /// Allocates an empty memory region of the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>A reference to the memory region.</returns>
        public MemoryReference AllocateRegion(int length)
            => Allocator.AllocateRegion(length);

        /// <summary>
        /// Retrieves a <see cref="Span{T}"/> over a referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <returns>The <see cref="Span{T}"/>.</returns>
        public Span<byte> GetRegion(MemoryReference reference)
            => Allocator.GetRegion(reference);

        /// <summary>
        /// Enqueues a render event.
        /// </summary>
        /// <param name="renderEvent">The render event.</param>
        public void EnqueueEvent(in RenderEvent renderEvent)
            => RenderEvents.Add(renderEvent);
    }
}
