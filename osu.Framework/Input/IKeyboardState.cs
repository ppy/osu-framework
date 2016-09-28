// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;
using System.Collections.Generic;

namespace osu.Framework.Input
{
    public interface IKeyboardState
    {
        bool AltPressed { get; }
        bool ControlPressed { get; }
        bool ShiftPressed { get; }
        bool WinPressed { get; }

        IEnumerable<Key> Keys { get; }
    }
}
