// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.States;
using OpenTK.Input;

namespace osu.Framework.Input.StateChanges
{
    public class MouseButtonInput : ButtonInput<MouseButton>
    {
        public MouseButtonInput(IEnumerable<ButtonInputEntry<MouseButton>> entries)
            : base(entries)
        {
        }

        public MouseButtonInput(MouseButton button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public MouseButtonInput(ButtonStates<MouseButton> current, ButtonStates<MouseButton> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<MouseButton> GetButtonStates(InputState state) => state.Mouse.Buttons;

        protected override void Handle(IInputStateChangeHandler handler, InputState state, MouseButton button, ButtonStateChangeKind kind) =>
            handler.HandleMouseButtonStateChange(state, button, kind);
    }
}
