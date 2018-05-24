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
        /// The current values of all non-zero axes.
        /// </summary>
        IReadOnlyList<JoystickAxis> Axes { get; }

        /// <summary>
        /// Finds the current value of an axis.
        /// </summary>
        /// <param name="axisIndex">The index of the axis to find the value for.</param>
        /// <returns>The axis' current value.</returns>
        float AxisValue(int axisIndex);

        IJoystickState Clone();
    }
}
