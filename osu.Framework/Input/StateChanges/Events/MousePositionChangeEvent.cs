// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges.Events
{
    public class MousePositionChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The current mouse position.
        /// </summary>
        public readonly Vector2 CurrentPosition;

        /// <summary>
        /// The last mouse position.
        /// </summary>
        public readonly Vector2 LastPosition;

        public MousePositionChangeEvent(InputState state, IInput input, Vector2 currentPosition, Vector2 lastPosition)
            : base(state, input)
        {
            CurrentPosition = currentPosition;
            LastPosition = lastPosition;
        }
    }
}
