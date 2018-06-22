// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardKeyInput : ButtonInput<Key>
    {
        public KeyboardKeyInput(Key button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public KeyboardKeyInput(ButtonStates<Key> current, ButtonStates<Key> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<Key> GetButtonStates(InputState state) => state.Keyboard.Keys;

        protected override void Handle(IInputStateChangeHandler handler, InputState state, Key key, ButtonStateChangeKind kind) =>
            handler.HandleKeyboardKeyStateChange(state, key, kind);
    }
}
