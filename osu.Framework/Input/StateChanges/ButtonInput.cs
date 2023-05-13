// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
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
        public ImmutableArray<ButtonInputEntry<TButton>> Entries;

        protected ButtonInput(IEnumerable<ButtonInputEntry<TButton>> entries)
        {
            Entries = entries.ToImmutableArray();
        }

        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> with a single <typeparamref name="TButton"/> state.
        /// </summary>
        /// <param name="button">The <typeparamref name="TButton"/> to add.</param>
        /// <param name="isPressed">The state of <paramref name="button"/>.</param>
        protected ButtonInput(TButton button, bool isPressed)
        {
            Entries = ImmutableArray.Create(new ButtonInputEntry<TButton>(button, isPressed));
        }

        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> from the difference of two <see cref="ButtonStates{TButton}"/>.
        /// </summary>
        /// <remarks>
        /// Buttons that are pressed in <paramref name="previous"/> and not pressed in <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Buttons that are not pressed in <paramref name="previous"/> and pressed in <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
        /// </remarks>
        /// <param name="current">The newer <see cref="ButtonStates{TButton}"/>.</param>
        /// <param name="previous">The older <see cref="ButtonStates{TButton}"/>.</param>
        protected ButtonInput(ButtonStates<TButton> current, ButtonStates<TButton> previous)
        {
            var difference = (current ?? new ButtonStates<TButton>()).EnumerateDifference(previous ?? new ButtonStates<TButton>());

            var builder = ImmutableArray.CreateBuilder<ButtonInputEntry<TButton>>(difference.Released.Length + difference.Pressed.Length);

            foreach (var button in difference.Released)
                builder.Add(new ButtonInputEntry<TButton>(button, false));
            foreach (var button in difference.Pressed)
                builder.Add(new ButtonInputEntry<TButton>(button, true));

            Entries = builder.MoveToImmutable();
        }

        /// <summary>
        /// Retrieves the <see cref="ButtonStates{TButton}"/> from an <see cref="InputState"/>.
        /// </summary>
        protected abstract ButtonStates<TButton> GetButtonStates(InputState state);

        /// <summary>
        /// Create a <typeparamref name="TButton"/> state change event.
        /// </summary>
        /// <param name="state">The <see cref="InputState"/> which changed.</param>
        /// <param name="button">The <typeparamref name="TButton"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="button"/>.</param>
        protected virtual ButtonStateChangeEvent<TButton> CreateEvent(InputState state, TButton button, ButtonStateChangeKind kind) => new ButtonStateChangeEvent<TButton>(state, this, button, kind);

        public virtual void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (Entries.Length == 0)
                return;

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
