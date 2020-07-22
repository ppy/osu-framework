// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using osu.Framework.Input.Handlers;

namespace osu.Framework.Input
{
    /// <summary>
    /// An <see cref="InputManager"/> implementation which allows managing of <see cref="InputHandler"/>s manually.
    /// </summary>
    public class CustomInputManager : InputManager
    {
        protected override ImmutableArray<InputHandler> InputHandlers => inputHandlers;

        private ImmutableArray<InputHandler> inputHandlers = ImmutableArray.Create<InputHandler>();

        protected void AddHandler(InputHandler handler)
        {
            if (!handler.Initialize(Host)) return;

            var existingHandlers = new List<InputHandler>(inputHandlers);

            // find the correct location to insert based on priority.
            int index = existingHandlers.BinarySearch(handler, new InputHandlerPriorityComparer());

            if (index < 0)
            {
                index = ~index;
            }

            existingHandlers.Insert(index, handler);
            inputHandlers = existingHandlers.ToImmutableArray();
        }

        protected void RemoveHandler(InputHandler handler)
        {
            inputHandlers = inputHandlers.Where(h => h != handler).ToImmutableArray();
        }

        protected override void Dispose(bool isDisposing)
        {
            foreach (var h in inputHandlers)
                h.Dispose();

            base.Dispose(isDisposing);
        }

        private class InputHandlerPriorityComparer : IComparer<InputHandler>
        {
            public int Compare(InputHandler h1, InputHandler h2)
            {
                if (h1 == null) throw new ArgumentNullException(nameof(h1));
                if (h2 == null) throw new ArgumentNullException(nameof(h2));

                return h2.Priority.CompareTo(h1.Priority);
            }
        }
    }
}
