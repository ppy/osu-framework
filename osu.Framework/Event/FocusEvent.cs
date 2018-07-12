// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Event
{
    public class FocusEvent : MouseEvent
    {
        public FocusEvent(InputState state)
            : base(state)
        {
        }
    }
}
