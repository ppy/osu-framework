// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input
{
    /// <summary>
    /// An abstract base class of an <see cref="IInput"/> which denotes a list of button state changes (pressed or released).
    /// </summary>
    /// <typeparam name="TButton">The type of button.</typeparam>
    public abstract class ButtonInput<TButton> : IInput
        where TButton : struct
    {
        public IEnumerable<ButtonInputEntry<TButton>> Entries;

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
            var difference = current.EnumerateDifference(previous ?? new ButtonStates<TButton>());

            Entries = difference.Released.Select(button => new ButtonInputEntry<TButton>(button, false))
                                .Concat(difference.Pressed.Select(button => new ButtonInputEntry<TButton>(button, true)));
        }

        /// <summary>
        /// Retrieves the <see cref="ButtonStates{TButton}"/> from an <see cref="InputState"/>.
        /// </summary>
        protected abstract ButtonStates<TButton> GetButtonStates(InputState state);

        /// <summary>
        /// Handles a <see cref="TButton"/> state change.
        /// This can be used to invoke the <see cref="IInputStateChangeHandler"/>'s HandleXXXStateChange methods.
        /// </summary>
        /// <param name="handler">The <see cref="IInputStateChangeHandler"/> that should handle the <see cref="InputState"/> change.</param>
        /// <param name="state">The <see cref="InputState"/> which changed.</param>
        /// <param name="button">The <see cref="TButton"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="button"/>.</param>
        protected abstract void Handle(IInputStateChangeHandler handler, InputState state, TButton button, ButtonStateChangeKind kind);

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var buttonStates = GetButtonStates(state);
            foreach (var entry in Entries)
            {
                if (buttonStates.SetPressed(entry.Button, entry.IsPressed))
                {
                    Handle(handler, state, entry.Button, entry.IsPressed ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
                }
            }
        }
    }
}
