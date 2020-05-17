// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        public readonly JoystickAxis Axis;

        public JoystickAxisMoveEvent([NotNull] InputState state, JoystickAxis axis)
            : base(state)
        {
            Axis = axis;
        }

        /// <summary>
        /// List of joystick axes.
        /// </summary>
        public IReadOnlyList<float> Axes => CurrentState.Joystick.Axes;

        public override string ToString() => $"{GetType().ReadableName()}({Axis})";
    }
}
