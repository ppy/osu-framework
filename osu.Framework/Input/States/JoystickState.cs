// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

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
        /// Retrieves all <see cref="JoystickAxis"/> with their current value (regardless of inactive ones).
        /// </summary>
        public IEnumerable<JoystickAxis> GetAxes() =>
            AxesValues.Select((v, i) => new JoystickAxis((JoystickAxisSource)i, v));
    }
}
