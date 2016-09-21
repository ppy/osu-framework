// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using OpenTK.Input;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input
{
    public class KeyboardState : IKeyboardState
    {
        public IKeyboardState LastState;

        public IEnumerable<Key> Keys { get; internal set; } = new Key[] { };

        public KeyboardState(IKeyboardState last = null)
        {
            LastState = last;
        }

        public bool ControlPressed => Keys.Any(k => k == Key.LControl || k == Key.RControl);
        public bool AltPressed => Keys.Any(k => k == Key.LAlt || k == Key.RAlt);
        public bool ShiftPressed => Keys.Any(k => k == Key.LShift || k == Key.RShift);
    }
}
