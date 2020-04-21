// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;

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
        public TouchSource Source => LastTouch.Source;

        public TouchPositionChangeEvent(InputState state, IInput input, Touch lastTouch)
            : base(state, input)
        {
            LastTouch = lastTouch;
        }
    }
}
