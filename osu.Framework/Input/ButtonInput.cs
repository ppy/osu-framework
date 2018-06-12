// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input
{
    /// <summary>
    /// An abstract base class of an <see cref="IInput"/> which denotes a list of button state changes (pressed or released).
    /// </summary>
    /// <typeparam name="TButton">Type of button</typeparam>
    public abstract class ButtonInput<TButton> : IInput
    where TButton : struct
    {
        public IEnumerable<ButtonInputEntry<TButton>> Entries;

        /// <summary>
        /// Get a <see cref="ButtonStates{TButton}"/> from an <see cref="InputState"/>.
        /// </summary>
        protected abstract ButtonStates<TButton> GetButtonStates(InputState state);

        /// <summary>
        /// Handle a button state change.
        /// It can be used to call handler's HandleXXXStateChange with <paramref name="state"/>, <paramref name="button"/> and <paramref name="kind"/>.
        /// </summary>
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

    /// <summary>
    /// Denotes a state of a button.
    /// </summary>
    /// <typeparam name="TButton">Type of button</typeparam>
    public struct ButtonInputEntry<TButton>
    where TButton : struct
    {
        /// <summary>
        /// The button it referring to.
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

    internal static class ButtonInputHelper
    {
        /// <summary>
        /// Create a <see cref="ButtonInput{TButton}"/> with a single entry.
        /// </summary>
        public static TInput MakeInput<TInput, TButton>(TButton button, bool isPressed)
        where TButton : struct
        where TInput : ButtonInput<TButton>, new() => new TInput
        {
            Entries = new[] { new ButtonInputEntry<TButton>(button, isPressed) }
        };

        /// <summary>
        /// Create a <see cref="ButtonInput{TButton}"/> from the difference of two states.
        /// <para>
        /// Buttons that is pressed in <paramref name="last"/> and not pressed <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Buttons that is not pressed in <paramref name="last"/> and pressed <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
        /// </para>
        /// </summary>
        public static TInput TakeDifference<TInput, TButton>(ButtonStates<TButton> current, ButtonStates<TButton> last)
            where TButton : struct
            where TInput : ButtonInput<TButton>, new()
        {
            var difference = current.EnumerateDifference(last ?? new ButtonStates<TButton>());
            return new TInput
            {
                Entries =
                    difference.Released.Select(button => new ButtonInputEntry<TButton>(button, false)).Concat(
                        difference.Pressed.Select(button => new ButtonInputEntry<TButton>(button, true)))
            };
        }
    }
}
