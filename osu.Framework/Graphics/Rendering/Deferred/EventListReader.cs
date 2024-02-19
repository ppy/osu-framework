// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    /// <summary>
    /// Reads an <see cref="EventList"/>. Semantically, this is very similar to <see cref="IEnumerator{T}"/>.
    /// </summary>
    internal ref struct EventListReader
    {
        private readonly ResourceAllocator allocator;
        private readonly List<MemoryReference> events = new List<MemoryReference>();

        private int eventIndex;
        private Span<byte> eventData = Span<byte>.Empty;

        public EventListReader(ResourceAllocator allocator, List<MemoryReference> events)
        {
            this.allocator = allocator;
            this.events = events;
            eventIndex = 0;
        }

        /// <summary>
        /// Advances to the next (or first) event in the list.
        /// </summary>
        /// <returns>Whether an event can be read.</returns>
        public bool Next()
        {
            if (eventIndex < events.Count)
            {
                eventData = allocator.GetRegion(events[eventIndex]);
                eventIndex++;
                return true;
            }

            eventData = Span<byte>.Empty;
            return false;
        }

        /// <summary>
        /// Reads the current event type.
        /// </summary>
        /// <remarks>
        /// Not valid for use if <see cref="Next"/> returns <c>false</c>.
        /// </remarks>
        public readonly ref RenderEventType CurrentType()
            => ref MemoryMarshal.AsRef<RenderEventType>(eventData);

        /// <summary>
        /// Reads the current event.
        /// </summary>
        /// <typeparam name="T">The expected event type.</typeparam>
        /// <remarks>
        /// Not valid for use if <see cref="Next"/> returns <c>false</c>.
        /// </remarks>
        public readonly ref T Current<T>()
            where T : unmanaged, IRenderEvent
            => ref MemoryMarshal.AsRef<T>(eventData[1..]);

        /// <summary>
        /// The index of the current event in the list.
        /// </summary>
        public int CurrentIndex()
            => eventIndex - 1;

        /// <summary>
        /// Resets this <see cref="EventListReader"/> to the start of the list.
        /// </summary>
        public void Reset()
        {
            eventIndex = 0;
            eventData = Span<byte>.Empty;
        }
    }
}
