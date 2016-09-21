// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IKeyboardState
    {
        bool AltPressed { get; }
        bool ControlPressed { get; }
        bool ShiftPressed { get; }

        ReadOnlyList<Key> Keys { get; }
    }
}