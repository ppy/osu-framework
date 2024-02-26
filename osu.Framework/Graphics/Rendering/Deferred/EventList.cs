// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private MemoryReference createEvent<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            MemoryReference reference = allocator.AllocateRegion(Unsafe.SizeOf<T>());
            Span<byte> buffer = allocator.GetRegion(reference);

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), renderEvent);

            return reference;
        }

        /// <summary>
        /// Creates a reader of this <see cref="EventList"/>.
        /// </summary>
        /// <returns>The <see cref="Enumerator"/>.</returns>
        public Enumerator CreateEnumerator()
            => new Enumerator(this);

        /// <summary>
        /// Reads an <see cref="EventList"/>. Semantically, this is very similar to <see cref="IEnumerator{T}"/>.
        /// </summary>
        internal ref struct Enumerator
        {
            private readonly EventList list;

            private int eventIndex;
            private Span<byte> eventData = Span<byte>.Empty;

            public Enumerator(EventList list)
            {
                this.list = list;
                eventIndex = 0;
            }

            /// <summary>
            /// Advances to the next (or first) event in the list.
            /// </summary>
            /// <returns>Whether an event can be read.</returns>
            public bool Next()
            {
                if (eventIndex < list.events.Count)
                {
                    eventData = list.allocator.GetRegion(list.events[eventIndex]);
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
            public readonly RenderEventType CurrentType()
                => (RenderEventType)eventData[0];

            /// <summary>
            /// Reads the current event.
            /// </summary>
            /// <typeparam name="T">The expected event type.</typeparam>
            /// <remarks>
            /// Not valid for use if <see cref="Next"/> returns <c>false</c>.
            /// </remarks>
            public readonly ref T Current<T>()
                where T : unmanaged, IRenderEvent
                => ref MemoryMarshal.AsRef<T>(eventData);

            /// <summary>
            /// Replaces the current event with a new one.
            /// </summary>
            /// <param name="newEvent">The new render event.</param>
            /// <typeparam name="T">The new event type.</typeparam>
            public void Replace<T>(T newEvent)
                where T : unmanaged, IRenderEvent
            {
                if (Unsafe.SizeOf<T>() <= eventData.Length)
                {
                    // Fast path where we can maintain contiguous data reads.
                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(eventData), newEvent);
                }
                else
                {
                    // Slow path.
                    eventData = list.allocator.GetRegion(list.events[eventIndex] = list.createEvent(newEvent));
                }
            }
        }
    }
}
