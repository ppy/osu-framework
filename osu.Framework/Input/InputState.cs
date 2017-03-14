// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Input
{
    public class InputState : EventArgs
    {
        public IKeyboardState Keyboard;
        public IMouseState Mouse;
        public InputState Last;

        /// <summary>
        /// Sometimes InputStates may be modified/transformed in game code.
        /// This will return the original untransformed state.
        /// </summary>
        public readonly InputState OriginalState;

        public InputState() : this(null)
        {
        }

        protected InputState(InputState original = null)
        {
            OriginalState = original ?? this;
        }

        public InputState Clone()
        {
            return new InputState(OriginalState)
            {
                Keyboard = Keyboard,
                Mouse = Mouse,
                Last = Last
            };
        }
    }
}
