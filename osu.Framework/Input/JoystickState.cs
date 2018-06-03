// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input
{
    public class JoystickState : IJoystickState
    {
        public IEnumerable<JoystickButton> Buttons { get; set; } = Array.Empty<JoystickButton>();
        public IReadOnlyList<JoystickAxis> Axes { get; set; } = Array.Empty<JoystickAxis>();

        public float AxisValue(int axisIndex) => Axes.FirstOrDefault(a => a.Axis == axisIndex).Value;

        public IJoystickState Clone()
        {
            var clone = (JoystickState)MemberwiseClone();
            clone.Buttons = new List<JoystickButton>(Buttons);
            clone.Axes = new List<JoystickAxis>(Axes);

            return clone;
        }
    }
}
