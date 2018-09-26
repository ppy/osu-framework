// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a joystick button.
    /// </summary>
    public abstract class JoystickButtonEvent : UIEvent
    {
        public readonly JoystickButton Button;

        protected JoystickButtonEvent(InputState state, JoystickButton button)
            : base(state)
        {
            Button = button;
        }

        /// <summary>
        /// List of currently pressed joystick buttons.
        /// </summary>
        public IEnumerable<JoystickButton> PressedJoystickButtons => CurrentState.Joystick.Buttons;

        /// <summary>
        /// List of joystick axes. Axes which have zero value may be omitted.
        /// </summary>
        public IEnumerable<JoystickAxis> JoystickAxes => CurrentState.Joystick.Axes;

        public override string ToString() => $"{GetType().ReadableName()}({Button})";
    }
}
