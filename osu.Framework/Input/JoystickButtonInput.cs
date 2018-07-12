// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public class JoystickButtonInput : ButtonInput<JoystickButton>
    {
        public JoystickButtonInput(IEnumerable<ButtonInputEntry<JoystickButton>> entries)
            : base(entries)
        {
        }

        public JoystickButtonInput(JoystickButton button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public JoystickButtonInput(ButtonStates<JoystickButton> current, ButtonStates<JoystickButton> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<JoystickButton> GetButtonStates(InputState state) => state.Joystick.Buttons;
    }
}
