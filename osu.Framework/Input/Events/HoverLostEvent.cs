// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class HoverLostEvent : MouseEvent
    {
        public HoverLostEvent(InputState state)
            : base(state)
        {
        }
    }
}
