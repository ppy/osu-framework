// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// <inheritdoc cref="Input.KeyboardKey.Key"/>
        public readonly Key Key;

        /// <inheritdoc cref="Input.KeyboardKey.Character"/>
        public readonly char Character;

        /// <inheritdoc cref="States.KeyboardState.IsPressed(osuTK.Input.Key)"/>
        public bool IsPressed(Key key) => CurrentState.Keyboard.IsPressed(key);

        /// <inheritdoc cref="States.KeyboardState.IsPressed(char)"/>
        public bool IsPressed(char character) => CurrentState.Keyboard.IsPressed(character);

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
            Character = state.Keyboard.Characters[key];
        }

        public override string ToString() => $"{GetType().ReadableName()}({KeyboardKey.ToString(Key, Character)})";
    }
}
