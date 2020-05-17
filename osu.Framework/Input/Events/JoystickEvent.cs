// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public abstract class JoystickEvent : UIEvent
    {
        protected JoystickEvent([NotNull] InputState state)
            : base(state)
        {
        }

        /// <summary>
        /// List of currently pressed joystick buttons.
        /// </summary>
        public IEnumerable<JoystickButton> PressedButtons => CurrentState.Joystick.Buttons;

        /// <summary>
        /// List of joystick axes.
        /// </summary>
        public IReadOnlyList<float> Axes => CurrentState.Joystick.Axes;
    }
}
