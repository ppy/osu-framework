// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class TouchUpEvent : TouchEvent
    {
        public TouchUpEvent(InputState state, Touch touch, Vector2? screenSpaceTouchDownPosition)
            : base(state, touch, screenSpaceTouchDownPosition)
        {
        }
    }
}
