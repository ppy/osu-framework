// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    public class MousePositionAbsoluteInput : IInput
    {
        public Vector2 Position;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (!state.Mouse.IsPositionValid || state.Mouse.Position != Position)
            {
                state.Mouse.IsPositionValid = true;
                state.Mouse.Position = Position;
                handler.HandleMousePositionChange(state);
            }
        }
    }
}
