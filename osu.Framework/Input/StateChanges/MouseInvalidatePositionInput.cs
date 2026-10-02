// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an invalidation of the current mouse position,
    /// mainly used when a single remaining touch source is released,
    /// or a hovering pen (e.g. Apple Pencil) leaves the screen area.
    /// </summary>
    public class MouseInvalidatePositionInput : IInput
    {
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;

            if (mouse.IsPositionValid)
            {
                mouse.IsPositionValid = false;
                mouse.LastSource = this;
                handler.HandleInputStateChange(new MousePositionChangeEvent(state, this, mouse.Position));
            }
        }
    }
}
