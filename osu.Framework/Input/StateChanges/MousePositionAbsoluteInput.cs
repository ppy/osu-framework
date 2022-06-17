// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an absolute change of mouse position.
    /// Pointing devices such as tablets provide absolute input.
    /// </summary>
    /// <remarks>
    /// This is the first input received from any pointing device.
    /// </remarks>
    public class MousePositionAbsoluteInput : IInput
    {
        /// <summary>
        /// The position which will be assigned to the current position.
        /// </summary>
        public Vector2 Position;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;

            if (!mouse.IsPositionValid || mouse.Position != Position)
            {
                var lastPosition = mouse.IsPositionValid ? mouse.Position : Position;
                mouse.IsPositionValid = true;
                mouse.LastSource = this;
                mouse.Position = Position;
                handler.HandleInputStateChange(new MousePositionChangeEvent(state, this, lastPosition));
            }
        }
    }
}
