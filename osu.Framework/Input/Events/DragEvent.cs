// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a mouse drag.
    /// Triggered when mouse is moved while dragging.
    /// </summary>
    public class DragEvent : MouseButtonEvent
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

        public DragEvent(InputState state, MouseButton button, Vector2? screenSpaceMousePosition = null, Vector2? screenSpaceLastMousePosition = null)
            : base(state, button, screenSpaceMousePosition)
        {
            ScreenSpaceLastMousePosition = screenSpaceLastMousePosition ?? state.Mouse.Position;
        }
    }
}
