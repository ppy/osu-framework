// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using osu.Framework.Input.States;

namespace osu.Framework.Event
{
    public class JoystickPressEvent : JoystickButtonEvent
    {
        public JoystickPressEvent(InputState state, JoystickButton button)
            : base(state, button)
        {
        }
    }
}
