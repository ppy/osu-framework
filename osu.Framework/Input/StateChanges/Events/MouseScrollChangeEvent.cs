// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using OpenTK;

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
