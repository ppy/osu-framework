// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges.Events
{
    public class TouchPositionChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The touch structure at the last position.
        /// </summary>
        public readonly Touch LastTouch;

        /// <summary>
        /// The touch source of this change event.
        /// </summary>
        public MouseButton Source => LastTouch.Source;

        public TouchPositionChangeEvent(InputState state, IInput input, Touch lastTouch)
            : base(state, input)
        {
            if (lastTouch.Source < MouseButton.Touch1 || lastTouch.Source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid source provided within a touch: {lastTouch.Source}", nameof(lastTouch));

            LastTouch = lastTouch;
        }
    }
}
