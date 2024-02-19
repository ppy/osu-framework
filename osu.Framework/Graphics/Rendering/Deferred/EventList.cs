// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventList
    {
        private readonly ResourceAllocator allocator;
        private readonly List<MemoryReference> events = new List<MemoryReference>();

        public EventList(ResourceAllocator allocator)
        {
            this.allocator = allocator;
        }

        /// <summary>
        /// Prepares this <see cref="EventList"/> for a new frame.
        /// </summary>
        public void NewFrame()
            => events.Clear();

        /// <summary>
        /// Enqueues a render event to the list.
        /// </summary>
        /// <param name="renderEvent">The render event.</param>
        /// <typeparam name="T">The event type.</typeparam>
        public void Enqueue<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
            => events.Add(createEvent(renderEvent));

        /// <summary>
        /// Replaces the current event referenced by the <see cref="EventListReader"/> with a new one.
        /// </summary>
        /// <param name="reader">The <see cref="EventListReader"/>.</param>
        /// <param name="newEvent">The new render event.</param>
        /// <typeparam name="T">The new event type.</typeparam>
        public void ReplaceCurrent<T>(EventListReader reader, in T newEvent)
            where T : unmanaged, IRenderEvent
            => events[reader.CurrentIndex()] = createEvent(newEvent);

        private MemoryReference createEvent<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            int requiredSize = Unsafe.SizeOf<T>() + 1;

            MemoryReference reference = allocator.AllocateRegion(requiredSize);
            Span<byte> buffer = allocator.GetRegion(reference);

            buffer[0] = (byte)renderEvent.Type;
            Unsafe.WriteUnaligned(ref buffer[1], renderEvent);

            return reference;
        }

        /// <summary>
        /// Creates a reader of this <see cref="EventList"/>.
        /// </summary>
        /// <returns>The <see cref="EventListReader"/>.</returns>
        public EventListReader CreateReader()
            => new EventListReader(allocator, events);
    }
}
