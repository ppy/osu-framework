// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventList
    {
        private readonly List<RenderEvent> events = new List<RenderEvent>();

        /// <summary>
        /// Prepares this <see cref="EventList"/> for a new frame.
        /// </summary>
        public void NewFrame()
            => events.Clear();

        /// <summary>
        /// Enqueues a render event to the list.
        /// </summary>
        /// <param name="renderEvent">The render event.</param>
        public void Enqueue(RenderEvent renderEvent)
            => events.Add(renderEvent);

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
            private RenderEvent currentEvent;

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
                    currentEvent = list.events[eventIndex];
                    eventIndex++;
                    return true;
                }

                return false;
            }

            public RenderEvent Current() => currentEvent;

            /// <summary>
            /// Replaces the current event with a new one.
            /// </summary>
            /// <param name="newEvent">The new render event.</param>
            public void Replace(RenderEvent newEvent)
            {
                list.events[eventIndex - 1] = newEvent;
            }
        }
    }
}
