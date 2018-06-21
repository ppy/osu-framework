// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes multiple button states.
    /// </summary>
    /// <typeparam name="TButton">The type of button.</typeparam>
    public class ButtonStates<TButton> : IEnumerable<TButton>
        where TButton : struct
    {
        private List<TButton> pressedButtons = new List<TButton>();

        public ButtonStates<TButton> Clone()
        {
            var clone = (ButtonStates<TButton>)MemberwiseClone();
            clone.pressedButtons = new List<TButton>(pressedButtons);
            return clone;
        }

        /// <summary>
        /// Finds whether a <see cref="TButton"/> is currently pressed.
        /// </summary>
        /// <param name="button">The <see cref="TButton"/> to check.</param>
        public bool IsPressed(TButton button) => pressedButtons.Contains(button);

        /// <summary>
        /// Sets the state of a <see cref="TButton"/>.
        /// </summary>
        /// <param name="button">The <see cref="TButton"/> to set the state of.</param>
        /// <param name="pressed">Whether <paramref name="button"/> should be pressed.</param>
        /// <returns>Whether the state of <paramref name="button"/> actually changed.</returns>
        public bool SetPressed(TButton button, bool pressed)
        {
            if (pressedButtons.Contains(button) == pressed)
                return false;

            if (pressed)
                pressedButtons.Add(button);
            else
                pressedButtons.Remove(button);
            return true;
        }

        public bool HasAnyButtonPressed => pressedButtons.Any();

        /// <summary>
        /// Enumerates the differences between ourselves and a previous <see cref="ButtonStates{TButton}"/>.
        /// </summary>
        /// <param name="lastButtons">The previous <see cref="ButtonStates{TButton}"/>.</param>
        public (IEnumerable<TButton> Released, IEnumerable<TButton> Pressed) EnumerateDifference(ButtonStates<TButton> lastButtons)
        {
            return (lastButtons.pressedButtons.Except(pressedButtons), pressedButtons.Except(lastButtons.pressedButtons));
        }

        /// <summary>
        /// Copies the state of another <see cref="ButtonStates{TButton}"/> to ourselves.
        /// </summary>
        /// <param name="other">The <see cref="ButtonStates{TButton}"/> to copy.</param>
        public void Set(ButtonStates<TButton> other)
        {
            pressedButtons.Clear();
            pressedButtons.AddRange(other.pressedButtons);
        }

        public override string ToString()
        {
            return $@"{GetType().ReadableName()}({String.Join(" ", pressedButtons)})";
        }

        public IEnumerator<TButton> GetEnumerator()
        {
            return ((IEnumerable<TButton>)pressedButtons).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TButton>)pressedButtons).GetEnumerator();
        }

        // for collection initializer
        public void Add(TButton button) => SetPressed(button, true);
    }
}
