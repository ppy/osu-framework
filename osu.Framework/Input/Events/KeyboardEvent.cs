// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a keyboard key.
    /// </summary>
    public abstract class KeyboardEvent : UIEvent
    {
        public readonly Key Key;

        /// <summary>
        /// Whether a specific key is pressed.
        /// </summary>
        public bool IsPressed(Key key) => CurrentState.Keyboard.Keys.IsPressed(key);

        /// <summary>
        /// Whether any key is pressed.
        /// </summary>
        public bool HasAnyKeyPressed => CurrentState.Keyboard.Keys.HasAnyButtonPressed;

        /// <summary>
        /// List of currently pressed keys.
        /// </summary>
        public IEnumerable<Key> PressedKeys => CurrentState.Keyboard.Keys;

        protected KeyboardEvent(InputState state, Key key)
            : base(state)
        {
            Key = key;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Key})";
    }
}
