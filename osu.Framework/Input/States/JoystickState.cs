// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input.States
{
    public class JoystickState
    {
        public readonly ButtonStates<JoystickButton> Buttons = new ButtonStates<JoystickButton>();
        public readonly List<JoystickAxis> Axes = new List<JoystickAxis>();
    }
}
