// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.Events
{
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
