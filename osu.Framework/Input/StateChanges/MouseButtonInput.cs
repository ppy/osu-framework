// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Input.States;
using osuTK.Input;

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

        public override void Apply(InputState state, IInputStateChangeHandler handler)
        {
            state.Mouse.LastSource = this;
            base.Apply(state, handler);
        }
    }
}
