// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public class JoystickState : IJoystickState
    {
        public IEnumerable<JoystickButton> Buttons { get; set; } = new List<JoystickButton>();

        public IReadOnlyList<float> Axes { get; set; } = new List<float>();

        public float AxisDelta(int axisIndex)
        {
            if (LastState == null)
                return Axes[axisIndex];
            return Axes[axisIndex] - LastState.Axes[axisIndex];
        }

        private IJoystickState lastState;
        public IJoystickState LastState
        {
            get => lastState;
            set
            {
                lastState = value;
                if (lastState != null) lastState.LastState = null;
            }
        }

        public IJoystickState Clone()
        {
            var clone = (JoystickState)MemberwiseClone();
            clone.Buttons = new List<JoystickButton>(Buttons);
            clone.Axes = new List<float>(Axes);
            clone.LastState = LastState;

            return clone;
        }
    }
}
