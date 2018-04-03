// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public interface IJoystickState
    {
        IReadOnlyList<int> Buttons { get; }

        IReadOnlyList<float> Axes { get; }

        float AxisDelta(int axisIndex);

        bool IsPressed(int buttonIndex);

        void SetPressed(int buttonIndex, bool pressed);

        // Todo: Hats?

        IJoystickState LastState { get; set; }

        IJoystickState Clone();
    }
}
