// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a relative change of mouse position.
    /// Pointing devices such as mice provide relative positional input.
    /// </summary>
    public class MousePositionRelativeInput : IInput
    {
        /// <summary>
        /// The change in position. This will be added to the current position.
        /// When the current position is not valid, no changes will be made.
        /// </summary>
        public Vector2 Delta;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;

            if (mouse.IsPositionValid && Delta != Vector2.Zero)
            {
                var lastPosition = mouse.Position;
                mouse.Position += Delta;
                mouse.LastSource = this;
                handler.HandleInputStateChange(new MousePositionChangeEvent(state, this, lastPosition));
            }
        }
    }
}
