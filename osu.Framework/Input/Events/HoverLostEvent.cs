// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing the end of mouse hover.
    /// Triggered when mouse cursor moved out of a drawable.
    /// </summary>
    public class HoverLostEvent : MouseEvent
    {
        public HoverLostEvent(InputState state)
            : base(state)
        {
        }
    }
}
