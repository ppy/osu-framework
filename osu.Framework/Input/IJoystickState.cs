// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public interface IJoystickState
    {
        IEnumerable<int> Buttons { get; }

        IEnumerable<float> Axes { get; }

        // Todo: Hats?

        IJoystickState Clone();
    }
}
