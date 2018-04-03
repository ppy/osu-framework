// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public class JoystickState : IJoystickState
    {
        private List<int> buttons = new List<int>();
        public IReadOnlyList<int> Buttons
        {
            get => buttons;
            set
            {
                buttons.Clear();
                buttons.AddRange(value);
            }
        }

        public IReadOnlyList<float> Axes { get; set; } = new List<float>();

        public float AxisDelta(int axisIndex)
        {
            if (LastState == null)
                return 0;
            return Axes[axisIndex] - LastState.Axes[axisIndex];
        }

        public bool IsPressed(int buttonIndex) => buttons.Contains(buttonIndex);

        public void SetPressed(int buttonIndex, bool pressed)
        {
            if (pressed)
            {
                if (IsPressed(buttonIndex))
                    return;
                buttons.Add(buttonIndex);
            }
            else
                buttons.Remove(buttonIndex);
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
            clone.buttons = new List<int>(buttons);
            clone.Axes = new List<float>(Axes);
            clone.LastState = LastState;

            return clone;
        }
    }
}
