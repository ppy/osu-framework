// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Input.States
{
    /// <summary>
    /// Denotes multiple button states.
    /// </summary>
    /// <typeparam name="TButton">The type of button.</typeparam>
    public class ButtonStates<TButton> : IEnumerable<TButton>
        where TButton : struct
    {
        private HashSet<TButton> pressedButtons = new HashSet<TButton>();

        public ButtonStates<TButton> Clone()
        {
            var clone = (ButtonStates<TButton>)MemberwiseClone();
            clone.pressedButtons = new HashSet<TButton>(pressedButtons);
            return clone;
        }

        /// <summary>
        /// Finds whether a <typeparamref name="TButton"/> is currently pressed.
        /// </summary>
        /// <param name="button">The <typeparamref name="TButton"/> to check.</param>
        public bool IsPressed(TButton button) => pressedButtons.Contains(button);

        /// <summary>
        /// Sets the state of a <typeparamref name="TButton"/>.
        /// </summary>
        /// <param name="button">The <typeparamref name="TButton"/> to set the state of.</param>
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

        public bool HasAnyButtonPressed => pressedButtons.Count > 0;

        /// <summary>
        /// Enumerates the differences between ourselves and a previous <see cref="ButtonStates{TButton}"/>.
        /// </summary>
        /// <param name="lastButtons">The previous <see cref="ButtonStates{TButton}"/>.</param>
        public ButtonStateDifference EnumerateDifference(ButtonStates<TButton> lastButtons)
        {
            if (!lastButtons.HasAnyButtonPressed)
            {
                // if no buttons pressed anywhere, use static to avoid alloc.
                return !HasAnyButtonPressed ? ButtonStateDifference.EMPTY : new ButtonStateDifference(Array.Empty<TButton>(), pressedButtons.ToArray());
            }

            if (!HasAnyButtonPressed)
                return new ButtonStateDifference(lastButtons.pressedButtons.ToArray(), Array.Empty<TButton>());

            List<TButton> released = new List<TButton>();
            List<TButton> pressed = new List<TButton>();

            foreach (var b in pressedButtons)
            {
                if (!lastButtons.pressedButtons.Contains(b))
                    pressed.Add(b);
            }

            foreach (var b in lastButtons.pressedButtons)
            {
                if (!pressedButtons.Contains(b))
                    released.Add(b);
            }

            return new ButtonStateDifference(released.ToArray(), pressed.ToArray());
        }

        /// <summary>
        /// Copies the state of another <see cref="ButtonStates{TButton}"/> to ourselves.
        /// </summary>
        /// <param name="other">The <see cref="ButtonStates{TButton}"/> to copy.</param>
        public void Set(ButtonStates<TButton> other)
        {
            pressedButtons.Clear();
            foreach (var b in other.pressedButtons)
                pressedButtons.Add(b);
        }

        public override string ToString() => $@"{GetType().ReadableName()}({string.Join(' ', pressedButtons)})";

        public IEnumerator<TButton> GetEnumerator() => ((IEnumerable<TButton>)pressedButtons).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // for collection initializer
        public void Add(TButton button) => SetPressed(button, true);

        public readonly struct ButtonStateDifference
        {
            public readonly TButton[] Released;
            public readonly TButton[] Pressed;

            public static readonly ButtonStateDifference EMPTY = new ButtonStateDifference(Array.Empty<TButton>(), Array.Empty<TButton>());

            public ButtonStateDifference(TButton[] released, TButton[] pressed)
            {
                Released = released;
                Pressed = pressed;
            }
        }
    }
}
