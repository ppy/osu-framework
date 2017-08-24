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

        public virtual InputState Clone()
        {
            var clone = (InputState)MemberwiseClone();
            clone.Keyboard = Keyboard?.Clone();
            clone.Mouse = Mouse?.Clone();
            clone.Last = Last;

            return clone;
        }
    }
}
