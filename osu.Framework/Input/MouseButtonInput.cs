// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseButtonInput : IInput
    {
        public MouseButton Button;
        public bool IsPressed;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Mouse.IsPressed(Button) != IsPressed)
            {
                state.Mouse.SetPressed(Button, IsPressed);
                handler.HandleMouseButtonStateChange(state, Button, IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
            }
        }
    }
}
