// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
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

        public readonly ref RenderEventType CurrentType()
            => ref MemoryMarshal.AsRef<RenderEventType>(eventData);

        public readonly ref T Current<T>()
            where T : unmanaged, IRenderEvent
            => ref MemoryMarshal.AsRef<T>(eventData[1..]);

        public int CurrentIndex()
            => eventIndex - 1;

        public void Reset()
        {
            eventIndex = 0;
            eventData = Span<byte>.Empty;
        }
    }
}
