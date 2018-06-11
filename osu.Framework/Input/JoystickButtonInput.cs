// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
{
    public class JoystickButtonInput : IInput
    {
        public JoystickButton Button;
        public bool IsPressed;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Joystick.Buttons.SetPressed(Button, IsPressed))
            {
                handler.HandleJoystickButtonStateChange(state, Button, IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
            }
        }
    }
}
