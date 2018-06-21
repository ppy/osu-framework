// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;

namespace osu.Framework.Input
{
    internal static class ButtonInputHelper
    {
        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> with a single entry.
        /// </summary>
        public static TInput MakeInput<TInput, TButton>(TButton button, bool isPressed)
            where TButton : struct
            where TInput : ButtonInput<TButton>, new() => new TInput
        {
            Entries = new[] { new ButtonInputEntry<TButton>(button, isPressed) }
        };

        /// <summary>
        /// Creates a <see cref="ButtonInput{TButton}"/> from the difference of two states.
        /// <para>
        /// Buttons that are pressed in <paramref name="last"/> and not pressed <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Released"/>.
        /// Buttons that are not pressed in <paramref name="last"/> and pressed <paramref name="current"/> will be listed as <see cref="ButtonStateChangeKind.Pressed"/>.
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
