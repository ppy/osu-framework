// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.States
{
    public class InputState : System.EventArgs
    {
        public IKeyboardState Keyboard;
        public IMouseState Mouse;
        public IJoystickState Joystick;

        public virtual InputState Clone()
        {
            var clone = (InputState)MemberwiseClone();
            clone.Keyboard = Keyboard?.Clone();
            clone.Mouse = Mouse?.Clone();
            clone.Joystick = Joystick?.Clone();
            return clone;
        }
    }
}
