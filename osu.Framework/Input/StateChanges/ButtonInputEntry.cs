// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a button state.
    /// </summary>
    /// <typeparam name="TButton">Type of button.</typeparam>
    public struct ButtonInputEntry<TButton>
        where TButton : struct
    {
        /// <summary>
        /// The button referred to.
        /// </summary>
        public TButton Button;

        /// <summary>
        /// Whether <see cref="Button"/> is currently pressed or not.
        /// </summary>
        public bool IsPressed;

        public ButtonInputEntry(TButton button, bool isPressed)
        {
            Button = button;
            IsPressed = isPressed;
        }
    }
}
