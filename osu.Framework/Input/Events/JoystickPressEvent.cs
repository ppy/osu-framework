// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a press of a joystick button.
    /// </summary>
    public class JoystickPressEvent : JoystickButtonEvent
    {
        public JoystickPressEvent(InputState state, JoystickButton button)
            : base(state, button)
        {
        }
    }
}
