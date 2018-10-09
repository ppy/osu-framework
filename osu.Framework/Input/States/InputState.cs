// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.States
{
    public class InputState
    {
        public readonly MouseState Mouse;
        public readonly KeyboardState Keyboard;
        public readonly JoystickState Joystick;

        public InputState(MouseState mouse = null, KeyboardState keyboard = null, JoystickState joystick = null)
        {
            Mouse = mouse ?? new MouseState();
            Keyboard = keyboard ?? new KeyboardState();
            Joystick = joystick ?? new JoystickState();
        }
    }
}
