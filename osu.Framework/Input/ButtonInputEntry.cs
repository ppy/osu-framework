// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
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
