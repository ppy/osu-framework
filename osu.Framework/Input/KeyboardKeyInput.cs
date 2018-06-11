// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardKeyInput : IInput
    {
        public Key Key;
        public bool IsPressed;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Keyboard.Keys.SetPressed(Key, IsPressed))
            {
                handler.HandleKeyboardKeyStateChange(state, Key, IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
            }
        }
    }
}
