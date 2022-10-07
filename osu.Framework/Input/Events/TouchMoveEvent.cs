// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represents a touch motion event.
    /// </summary>
    public class TouchMoveEvent : TouchEvent
    {
        /// <summary>
        /// The last touch position in the screen space.
        /// </summary>
        public readonly Vector2 ScreenSpaceLastTouchPosition;

        /// <summary>
        /// The last touch position in local space.
        /// </summary>
        public Vector2 LastTouchPosition => ToLocalSpace(ScreenSpaceLastTouchPosition);

        /// <summary>
        /// The difference of touch position from last position to current position in local space.
        /// </summary>
        public Vector2 Delta => Touch.Position - LastTouchPosition;

        public TouchMoveEvent(InputState state, Touch touch, Vector2? screenSpaceTouchDownPosition, Vector2 screenSpaceLastTouchPosition)
            : base(state, touch, screenSpaceTouchDownPosition)
        {
            ScreenSpaceLastTouchPosition = screenSpaceLastTouchPosition;
        }
    }
}
