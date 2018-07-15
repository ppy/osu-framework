// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IKeyboardState
    {
        ButtonStates<Key> Keys { get; }

        bool IsPressed(Key key);

        void SetPressed(Key key, bool pressed);

        bool AltPressed { get; }
        bool ControlPressed { get; }
        bool ShiftPressed { get; }

        /// <summary>
        /// Win key on Windows, or Command key on Mac.
        /// </summary>
        bool SuperPressed { get; }

        IKeyboardState Clone();
    }
}
