// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing the start of a mouse drag.
    /// </summary>
    public class DragStartEvent : MouseButtonEvent
    {
        /// <summary>
        /// The difference from mouse down position to current position in local space.
        /// </summary>
        public Vector2 Delta => MousePosition - MouseDownPosition;

        public DragStartEvent(InputState state, MouseButton button, Vector2? screenSpaceMouseDownPosition = null)
            : base(state, button, screenSpaceMouseDownPosition)
        {
        }
    }
}
