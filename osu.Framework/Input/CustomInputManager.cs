// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
