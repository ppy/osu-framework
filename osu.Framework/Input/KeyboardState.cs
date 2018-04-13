// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardState : IKeyboardState
    {
        public IEnumerable<Key> Keys { get; set; } = new Key[] { };

        public bool ControlPressed => Keys.Any(k => k == Key.LControl || k == Key.RControl);
        public bool AltPressed => Keys.Any(k => k == Key.LAlt || k == Key.RAlt);
        public bool ShiftPressed => Keys.Any(k => k == Key.LShift || k == Key.RShift);

        /// <summary>
        /// Win key on Windows, or Command key on Mac.
        /// </summary>
        public bool SuperPressed => Keys.Any(k => k == Key.LWin || k == Key.RWin);

        public IKeyboardState Clone()
        {
            var clone = (KeyboardState)MemberwiseClone();
            clone.Keys = new List<Key>(Keys);
            return clone;
        }
    }
}
