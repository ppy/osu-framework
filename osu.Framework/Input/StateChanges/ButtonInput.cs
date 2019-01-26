// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// An abstract base class of an <see cref="IInput"/> which denotes a list of button state changes (pressed or released).
    /// </summary>
    /// <typeparam name="TButton">The type of button.</typeparam>
    public abstract class ButtonInput<TButton> : IInput
        where TButton : struct
    {
        public IEnumerable<ButtonInputEntry<TButton>> Entries;

        protected ButtonInput(IEnumerable<ButtonInputEntry<TButton>> entries)
        {
            Entries = entries;
        }

        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> with a single <see cref="TButton"/> state.
        /// </summary>
        /// <param name="button">The <see cref="TButton"/> to add.</param>
        /// <param name="isPressed">The state of <paramref name="button"/>.</param>
        protected ButtonInput(TButton button, bool isPressed)
        {
            Entries = new[] { new ButtonInputEntry<TButton>(button, isPressed) };
        }

        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> from the difference of two <see cref="ButtonStates{TButton}"/>.
        /// </summary>
        /// <remarks>
        /// Buttons that are pressed in <paramref name="previous"/> and not pressed in <see cref="current"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Buttons that are not pressed in <paramref name="previous"/> and pressed in <see cref="current"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
        /// </remarks>
        /// <param name="current">The newer <see cref="ButtonStates{TButton}"/>.</param>
        /// <param name="previous">The older <see cref="ButtonStates{TButton}"/>.</param>
        protected ButtonInput(ButtonStates<TButton> current, ButtonStates<TButton> previous)
        {
            var difference = (current ?? new ButtonStates<TButton>()).EnumerateDifference(previous ?? new ButtonStates<TButton>());

            Entries = difference.Released.Select(button => new ButtonInputEntry<TButton>(button, false))
                                .Concat(difference.Pressed.Select(button => new ButtonInputEntry<TButton>(button, true)));
        }

        /// <summary>
        /// Retrieves the <see cref="ButtonStates{TButton}"/> from an <see cref="InputState"/>.
        /// </summary>
        protected abstract ButtonStates<TButton> GetButtonStates(InputState state);

        /// <summary>
        /// Create a <see cref="TButton"/> state change event.
        /// </summary>
        /// <param name="state">The <see cref="InputState"/> which changed.</param>
        /// <param name="button">The <see cref="TButton"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="button"/>.</param>
        protected virtual ButtonStateChangeEvent<TButton> CreateEvent(InputState state, TButton button, ButtonStateChangeKind kind)
        {
            return new ButtonStateChangeEvent<TButton>(state, this, button, kind);
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var buttonStates = GetButtonStates(state);
            foreach (var entry in Entries)
            {
                if (buttonStates.SetPressed(entry.Button, entry.IsPressed))
                {
                    var buttonStateChange = CreateEvent(state, entry.Button, entry.IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
                    handler.HandleInputStateChange(buttonStateChange);
                }
            }
        }
    }
}
