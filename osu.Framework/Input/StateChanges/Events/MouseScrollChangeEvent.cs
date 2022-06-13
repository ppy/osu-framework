// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges.Events
{
    public class MouseScrollChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The absolute value of scroll prior to this change.
        /// </summary>
        public readonly Vector2 LastScroll;

        /// <summary>
        /// Whether the change came from a device supporting precision scrolling.
        /// </summary>
        /// <remarks>
        /// In cases this is true, scroll events will generally map 1:1 to user's input, rather than incrementing in large "notches" (as expected of traditional scroll wheels).
        /// </remarks>
        public readonly bool IsPrecise;

        public MouseScrollChangeEvent(InputState state, IInput input, Vector2 lastScroll, bool isPrecise)
            : base(state, input)
        {
            LastScroll = lastScroll;
            IsPrecise = isPrecise;
        }
    }
}
