// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Input.States
{
    public class JoystickState
    {
        public const int MAX_AXES = 16;

        public readonly ButtonStates<JoystickButton> Buttons = new ButtonStates<JoystickButton>();

        /// <summary>
        /// The array of current values associated to each axis source.
        /// </summary>
        public readonly float[] AxesValues = new float[MAX_AXES];

        /// <summary>
        /// Currently simulated <see cref="JoystickButton"/> for each <see cref="JoystickAxis"/>. <c>0</c> if no button press is simulated.
        /// </summary>
        internal readonly JoystickButton[] AxisDirectionButtons = new JoystickButton[MAX_AXES];

        private readonly JoystickAxis[] axes = new JoystickAxis[MAX_AXES];

        /// <summary>
        /// Retrieves all <see cref="JoystickAxis"/> with their current value (regardless of inactive ones).
        /// </summary>
        public IEnumerable<JoystickAxis> GetAxes()
        {
            for (int i = 0; i < MAX_AXES; i++)
                axes[i] = new JoystickAxis((JoystickAxisSource)i, AxesValues[i]);

            return axes;
        }
    }
}
