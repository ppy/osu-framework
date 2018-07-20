// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using OpenTK;

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
                mouse.LastPosition = mouse.Position;
                mouse.Position += Delta;
                handler.HandleMousePositionChange(state);
            }
        }
    }
}
