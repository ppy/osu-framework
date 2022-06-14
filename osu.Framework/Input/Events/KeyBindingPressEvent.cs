// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a press of a key binding combination.
    /// </summary>
    /// <typeparam name="T">The action type.</typeparam>
    public class KeyBindingPressEvent<T> : KeyBindingEvent<T> where T : struct
    {
        /// <summary>
        /// Whether this is a repeated event resulted from a held keyboard key.
        /// </summary>
        public readonly bool Repeat;

        public KeyBindingPressEvent(InputState state, T action, bool repeat = false)
            : base(state, action)
        {
            Repeat = repeat;
        }
    }
}
