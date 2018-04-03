// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Input
{
    public interface IJoystickState
    {
        /// <summary>
        /// The currently pressed buttons.
        /// </summary>
        IEnumerable<JoystickButton> Buttons { get; }

        /// <summary>
        /// The current values of all axes.
        /// </summary>
        IReadOnlyList<float> Axes { get; }

        /// <summary>
        /// Finds the change in value of a specific axis.
        /// </summary>
        /// <param name="axisIndex">The index of the axis to find the delta for.</param>
        /// <returns>The change from the axis' last value.</returns>
        float AxisDelta(int axisIndex);

        // Todo: Hats?

        IJoystickState LastState { get; set; }

        IJoystickState Clone();
    }
}
