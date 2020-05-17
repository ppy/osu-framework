// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a joystick axis.
    /// </summary>
    public class JoystickAxisMoveEvent : UIEvent
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

        /// <summary>
        /// List of joystick axes.
        /// </summary>
        public IReadOnlyList<float> Axes => CurrentState.Joystick.Axes;

        public override string ToString() => $"{GetType().ReadableName()}({Axis})";
    }
}
