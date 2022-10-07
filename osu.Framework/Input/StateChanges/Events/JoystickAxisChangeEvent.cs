// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges.Events
{
    public class JoystickAxisChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The current joystick axis data.
        /// </summary>
        public readonly JoystickAxis Axis;

        /// <summary>
        /// The last value of this joystick axis.
        /// </summary>
        public readonly float LastValue;

        public JoystickAxisChangeEvent(InputState state, IInput input, JoystickAxis axis, float lastValue)
            : base(state, input)
        {
            Axis = axis;
            LastValue = lastValue;
        }
    }
}
