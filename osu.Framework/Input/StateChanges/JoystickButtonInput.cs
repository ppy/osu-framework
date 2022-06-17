// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
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
