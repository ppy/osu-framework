// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges.Events
{
    public class MouseScrollChangeEvent : InputStateChangeEvent
    {
        public readonly Vector2 LastScroll;

        public readonly bool IsPrecise;

        public MouseScrollChangeEvent(InputState state, IInput input, Vector2 lastScroll, bool isPrecise)
            : base(state, input)
        {
            LastScroll = lastScroll;
            IsPrecise = isPrecise;
        }
    }
}
