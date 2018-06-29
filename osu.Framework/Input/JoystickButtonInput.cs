// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
{
    public class JoystickButtonInput : ButtonInput<JoystickButton>
    {
        public JoystickButtonInput(JoystickButton button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public JoystickButtonInput(ButtonStates<JoystickButton> current, ButtonStates<JoystickButton> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<JoystickButton> GetButtonStates(InputState state) => state.Joystick.Buttons;

        protected override void Handle(IInputStateChangeHandler handler, InputState state, JoystickButton button, ButtonStateChangeKind kind) =>
            handler.HandleJoystickButtonStateChange(state, button, kind);
    }
}
