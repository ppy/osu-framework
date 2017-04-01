// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IKeyboardState
    {
        bool AltPressed { get; }
        bool ControlPressed { get; }
        bool ShiftPressed { get; }

        /// <summary>
        /// Win key on Windows, or Command key on Mac.
        /// </summary>
        bool SuperPressed { get; }

        IEnumerable<Key> Keys { get; }
    }
}
