// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseButtonInput : ButtonInput<MouseButton>
    {
        protected override ButtonStates<MouseButton> GetButtonStates(InputState state) => state.Mouse.Buttons;

        protected override void Handle(IInputStateChangeHandler handler, InputState state, MouseButton button, ButtonStateChangeKind kind) =>
            handler.HandleMouseButtonStateChange(state, button, kind);
    }
}
