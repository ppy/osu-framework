// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a keyboard key state.
    /// </summary>
    public struct KeyboardKeyInputEntry
    {
        /// <summary>
        /// The key referred to.
        /// </summary>
        public Key Key;

        /// <summary>
        /// Whether <see cref="Key"/> is currently pressed or not.
        /// </summary>
        public bool IsPressed;

        /// <summary>
        /// Whether this key is being repeated.
        /// </summary>
        public bool IsRepeated;

        public KeyboardKeyInputEntry(Key key, bool isPressed, bool isRepeated)
        {
            Key = key;
            IsPressed = isPressed;
            IsRepeated = isRepeated;
        }
    }
}
