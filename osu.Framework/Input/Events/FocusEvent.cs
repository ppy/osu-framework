// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event represeting that a drawable gained the focus.
    /// </summary>
    public class FocusEvent : UIEvent
    {
        public FocusEvent(InputState state)
            : base(state)
        {
        }
    }
}
