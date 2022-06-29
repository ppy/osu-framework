// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a change of the mouse wheel.
    /// </summary>
    public class ScrollEvent : MouseEvent
    {
        /// <summary>
        /// The relative change in scroll associated with this event.
        /// </summary>
        /// <remarks>
        /// Delta is positive when mouse wheel scrolled to the up or left, in non-"natural" scroll mode (ie. the classic way).
        /// </remarks>
        public readonly Vector2 ScrollDelta;

        /// <summary>
        /// Whether the change came from a device supporting precision scrolling.
        /// </summary>
        /// <remarks>
        /// In cases this is true, scroll events will generally map 1:1 to user's input, rather than incrementing in large "notches" (as expected of traditional scroll wheels).
        /// </remarks>
        public readonly bool IsPrecise;

        public ScrollEvent(InputState state, Vector2 scrollDelta, bool isPrecise = false)
            : base(state)
        {
            ScrollDelta = scrollDelta;
            IsPrecise = isPrecise;
        }

        public override string ToString() => $"{GetType().ReadableName()}({ScrollDelta}, {IsPrecise})";
    }
}
