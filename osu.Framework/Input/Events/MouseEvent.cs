// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represent an event which is propagated based on mouse position.
    /// </summary>
    public abstract class MouseEvent : UIEvent
    {
        /// <summary>
        /// Whether a specific mouse button is pressed.
        /// </summary>
        public bool IsPressed(MouseButton button) => CurrentState.Mouse.Buttons.IsPressed(button);

        /// <summary>
        /// Whether any mouse button is pressed.
        /// </summary>
        public bool HasAnyButtonPressed => CurrentState.Mouse.Buttons.HasAnyButtonPressed;

        /// <summary>
        /// List of currently pressed mouse buttons.
        /// </summary>
        public IEnumerable<MouseButton> PressedButtons => CurrentState.Mouse.Buttons;

        protected MouseEvent(InputState state)
            : base(state)
        {
        }
    }
}
