// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class TouchDownEvent : TouchEvent
    {
        public TouchDownEvent(InputState state, Touch touch)
            : base(state, touch, touch.Position)
        {
        }
    }
}
