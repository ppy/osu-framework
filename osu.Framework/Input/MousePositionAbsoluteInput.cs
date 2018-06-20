// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes an absolute change of mouse position.
    /// <para>
    /// An use case is when a current position is retrived from the platform.
    /// Another use case is when the user moves an absolutely positioned device such as a tablet.
    /// </para>
    /// </summary>
    public class MousePositionAbsoluteInput : IInput
    {
        /// <summary>
        /// The position which will be assigned to the current mouse position.
        /// </summary>
        public Vector2 Position;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;
            if (!mouse.IsPositionValid || mouse.Position != Position)
            {
                mouse.IsPositionValid = true;
                mouse.LastPosition = mouse.Position;
                mouse.Position = Position;
                handler.HandleMousePositionChange(state);
            }
        }
    }
}
