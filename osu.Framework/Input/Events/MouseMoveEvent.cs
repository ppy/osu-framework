// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a move of the mouse cursor.
    /// </summary>
    public class MouseMoveEvent : MouseEvent
    {
        /// <summary>
        /// The last mouse position before this mouse move in the screen space.
        /// </summary>
        public readonly Vector2 ScreenSpaceLastMousePosition;

        /// <summary>
        /// The last mouse position before this mouse move in local space.
        /// </summary>
        public Vector2 LastMousePosition => ToLocalSpace(ScreenSpaceLastMousePosition);

        /// <summary>
        /// The difference of mouse position from last position to current position in local space.
        /// </summary>
        public Vector2 Delta => MousePosition - LastMousePosition;

        public MouseMoveEvent(InputState state, Vector2? screenSpaceLastMousePosition = null)
            : base(state)
        {
            ScreenSpaceLastMousePosition = screenSpaceLastMousePosition ?? ScreenSpaceMousePosition;
        }
    }
}
