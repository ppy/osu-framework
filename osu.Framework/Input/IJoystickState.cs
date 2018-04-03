// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public interface IJoystickState
    {
        IEnumerable<JoystickButton> Buttons { get; }

        IReadOnlyList<float> Axes { get; }

        float AxisDelta(int axisIndex);

        // Todo: Hats?

        IJoystickState LastState { get; set; }

        IJoystickState Clone();
    }
}
