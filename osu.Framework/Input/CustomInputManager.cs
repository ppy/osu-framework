// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

            inputHandlers = inputHandlers.Append(handler).ToImmutableArray();
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
    }
}
