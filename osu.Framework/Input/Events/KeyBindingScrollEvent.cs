// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a change of a key binding's associated mouse wheel direction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeyBindingScrollEvent<T> : KeyBindingEvent<T> where T : struct
    {
        /// <summary>
        /// The relative change in scroll associated with this event.
        /// </summary>
        public readonly float ScrollAmount;

        /// <summary>
        /// Whether the change came from a device supporting precision scrolling.
        /// </summary>
        /// <remarks>
        /// In cases this is true, scroll events will generally map 1:1 to user's input, rather than incrementing in large "notches" (as expected of traditional scroll wheels).
        /// </remarks>
        public readonly bool IsPrecise;

        public KeyBindingScrollEvent(InputState state, T action, float amount, bool isPrecise = false)
            : base(state, action)
        {
            ScrollAmount = amount;
            IsPrecise = isPrecise;
        }
    }
}
