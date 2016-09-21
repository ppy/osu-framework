// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardState : IKeyboardState
    {
        public IKeyboardState LastState;

        public ReadOnlyList<Key> Keys { get; internal set; } = new ReadOnlyList<Key>();

        public KeyboardState(IKeyboardState last = null)
        {
            LastState = last;
        }

        public bool ControlPressed => Keys.Contains(Key.LControl) || Keys.Contains(Key.RControl);
        public bool AltPressed => Keys.Contains(Key.LAlt) || Keys.Contains(Key.RAlt);
        public bool ShiftPressed => Keys.Contains(Key.LShift) || Keys.Contains(Key.RShift);
    }
}
