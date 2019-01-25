// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Input
{
    public class UserInputManager : PassThroughInputManager
    {
        protected override IEnumerable<InputHandler> InputHandlers => Host.AvailableInputHandlers;

        protected override bool HandleHoverEvents => Host.Window?.CursorInWindow ?? true;

        public UserInputManager()
        {
            UseParentInput = false;
        }

        public override void HandleInputStateChange(InputStateChangeEvent inputStateChange)
        {
            switch (inputStateChange)
            {
                case MousePositionChangeEvent mousePositionChange:
                    var mouse = mousePositionChange.State.Mouse;
                    // confine cursor
                    if (Host.Window != null && Host.Window.CursorState.HasFlag(CursorState.Confined))
                        mouse.Position = Vector2.Clamp(mouse.Position, Vector2.Zero, new Vector2(Host.Window.Width, Host.Window.Height));
                    break;

                case MouseScrollChangeEvent _:
                    if (Host.Window != null && !Host.Window.CursorInWindow)
                        return;
                    break;
            }

            base.HandleInputStateChange(inputStateChange);
        }
    }
}
