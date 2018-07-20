// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input.States
{
    public class KeyboardState : IKeyboardState
    {
        public ButtonStates<Key> Keys { get; private set; } = new ButtonStates<Key>();

        public bool IsPressed(Key key) => Keys.IsPressed(key);
        public void SetPressed(Key key, bool pressed) => Keys.SetPressed(key, pressed);

        public bool ControlPressed => Keys.IsPressed(Key.LControl) || Keys.IsPressed(Key.RControl);
        public bool AltPressed => Keys.IsPressed(Key.LAlt) || Keys.IsPressed(Key.RAlt);
        public bool ShiftPressed => Keys.IsPressed(Key.LShift) || Keys.IsPressed(Key.RShift);

        /// <summary>
        /// Win key on Windows, or Command key on Mac.
        /// </summary>
        public bool SuperPressed => Keys.IsPressed(Key.LWin) || Keys.IsPressed(Key.RWin);

        public IKeyboardState Clone()
        {
            var clone = (KeyboardState)MemberwiseClone();
            clone.Keys = Keys.Clone();
            return clone;
        }
    }
}
