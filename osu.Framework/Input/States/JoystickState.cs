// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input.States
{
    public class JoystickState
    {
        public ButtonStates<JoystickButton> Buttons { get; private set; } = new ButtonStates<JoystickButton>();
        public IReadOnlyList<JoystickAxis> Axes { get; set; } = Array.Empty<JoystickAxis>();

        public float AxisValue(int axisIndex) => Axes.FirstOrDefault(a => a.Axis == axisIndex).Value;
    }
}
