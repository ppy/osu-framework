// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        public readonly Vector2 ScrollDelta;
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
