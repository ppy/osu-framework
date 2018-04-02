// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public class JoystickState : IJoystickState
    {
        public IEnumerable<int> Buttons { get; set;  } = new List<int>();
        public IEnumerable<float> Axes { get; set; } = new List<float>();

        public IJoystickState Clone()
        {
            var clone = (JoystickState)MemberwiseClone();
            clone.Buttons = new List<int>(Buttons);
            clone.Axes = new List<float>(Axes);

            return clone;
        }
    }
}
