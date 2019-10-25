// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.States
{
    public class InputState
    {
        public readonly MouseState Mouse;
        public readonly KeyboardState Keyboard;
        public readonly TouchState Touch;
        public readonly JoystickState Joystick;

        public InputState(MouseState mouse = null, KeyboardState keyboard = null, TouchState touch = null, JoystickState joystick = null)
        {
            Mouse = mouse ?? new MouseState();
            Keyboard = keyboard ?? new KeyboardState();
            Touch = touch ?? new TouchState();
            Joystick = joystick ?? new JoystickState();
        }
    }
}
