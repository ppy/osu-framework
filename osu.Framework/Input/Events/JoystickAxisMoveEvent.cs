// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a movement of a specific joystick axis.
    /// </summary>
    public class JoystickAxisMoveEvent : JoystickEvent
    {
        /// <summary>
        /// The current joystick axis data.
        /// </summary>
        public readonly JoystickAxis Axis;

        /// <summary>
        /// The last value of this joystick axis.
        /// </summary>
        public readonly float LastValue;

        /// <summary>
        /// The difference of axis value from last value to current value.
        /// </summary>
        public float Delta => Axis.Value - LastValue;

        public JoystickAxisMoveEvent([NotNull] InputState state, JoystickAxis axis, float lastValue)
            : base(state)
        {
            LastValue = lastValue;
            Axis = axis;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Axis.Source})";
    }
}
