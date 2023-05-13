// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK.Input;
using KeyboardState = osu.Framework.Input.States.KeyboardState;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// A class of an <see cref="IInput"/> which denotes a list of keyboard key state changes (pressed or released) and handles repeated key events.
    /// </summary>
    public class KeyboardKeyInput : IInput
    {
        public ImmutableArray<KeyboardKeyInputEntry> Entries;

        public KeyboardKeyInput(IEnumerable<KeyboardKeyInputEntry> entries)
        {
            Entries = entries.ToImmutableArray();
        }

        /// <summary>
        /// Creates a <see cref="KeyboardKeyInput"/> with a single <typeparamref name="Key"/> state.
        /// </summary>
        /// <param name="key">The <typeparamref name="Key"/> to add.</param>
        /// <param name="isPressed">The state of <paramref name="key"/>.</param>
        /// <param name="isRepeated">Whether this key event is being repeated.</param>
        public KeyboardKeyInput(Key key, bool isPressed, bool isRepeated)
        {
            Entries = ImmutableArray.Create(new KeyboardKeyInputEntry(key, isPressed, isRepeated));
        }

        /// <summary>
        /// Creates a <see cref="KeyboardKeyInput"/> from the difference of two <see cref="KeyboardState"/>s.
        /// </summary>
        /// <remarks>
        /// Keys that are pressed in <paramref name="previous"/> and not pressed in <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Keys that are not pressed in <paramref name="previous"/> and pressed in <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
        /// </remarks>
        /// <param name="current">The newer <see cref="KeyboardState"/>.</param>
        /// <param name="previous">The older <see cref="KeyboardState"/>.</param>
        public KeyboardKeyInput(KeyboardState current, KeyboardState previous)
        {
            var difference = (current.Keys ?? new ButtonStates<Key>()).EnumerateDifference(previous.Keys ?? new ButtonStates<Key>());

            var builder = ImmutableArray.CreateBuilder<KeyboardKeyInputEntry>(difference.Released.Length + difference.Pressed.Length);

            foreach (var button in difference.Released)
                builder.Add(new KeyboardKeyInputEntry(button, false, false));
            foreach (var button in difference.Pressed)
                builder.Add(new KeyboardKeyInputEntry(button, true, false));

            Entries = builder.MoveToImmutable();
        }

        /// <summary>
        /// Retrieves the <see cref="ButtonStates{Key}"/> from an <see cref="InputState"/>.
        /// </summary>
        protected ButtonStates<Key> GetKeyStates(InputState state)
        {
            return state.Keyboard.Keys;
        }

        /// <summary>
        /// Create a <typeparamref name="Key"/> state change event.
        /// </summary>
        /// <param name="state">The <see cref="InputState"/> which changed or is being repeated.</param>
        /// <param name="key">The <typeparamref name="Key"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="key"/>.</param>
        /// <param name="isRepeated">Whether this event is being repeated.</param>
        protected virtual KeyboardKeyStateChangeEvent CreateEvent(InputState state, Key key, ButtonStateChangeKind kind, bool isRepeated) => new KeyboardKeyStateChangeEvent(state, this, key, kind, isRepeated);

        public virtual void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (Entries.Length == 0)
                return;

            var keyStates = GetKeyStates(state);

            foreach (var entry in Entries)
            {
                if (keyStates.SetPressed(entry.Key, entry.IsPressed))
                {
                    var buttonStateChange = CreateEvent(state, entry.Key, entry.IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released, entry.IsRepeated);
                    handler.HandleInputStateChange(buttonStateChange);
                }
            }
        }
    }
}
