// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MousePositionAbsoluteChange : IInputHandlerResult
    {
        public Vector2 Position;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Mouse.Position != Position)
            {
                state.Mouse.Position = Position;
                handler.HandleMousePositionChange(state);
            }
        }
    }
}
