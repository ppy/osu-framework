// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Handlers;

namespace osu.Framework.Input
{
    /// <summary>
    /// An <see cref="InputManager"/> implementation which allows managing of <see cref="InputHandler"/>s manually.
    /// </summary>
    public class CustomInputManager : InputManager
    {
        protected override IEnumerable<InputHandler> InputHandlers => inputHandlers;

        private readonly List<InputHandler> inputHandlers = new List<InputHandler>();

        protected void AddHandler(InputHandler handler)
        {
            if (!handler.Initialize(Host)) return;

            int index = inputHandlers.BinarySearch(handler, new InputHandlerComparer());
            if (index < 0)
            {
                index = ~index;
            }

            inputHandlers.Insert(index, handler);
        }

        protected void RemoveHandler(InputHandler handler) => inputHandlers.Remove(handler);

        protected override void Dispose(bool isDisposing)
        {
            foreach (var h in inputHandlers)
                h.Dispose();

            base.Dispose(isDisposing);
        }
    }
}
