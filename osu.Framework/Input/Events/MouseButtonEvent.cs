// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represents events of a mouse button.
    /// </summary>
    public abstract class MouseButtonEvent : MouseEvent
    {
        /// <summary>
        /// The mouse button that fired this event.
        /// </summary>
        public readonly MouseButton Button;

        /// <summary>
        /// The mouse position at a <see cref="MouseDownEvent"/> in the screen space.
        /// </summary>
        public readonly Vector2 ScreenSpaceMouseDownPosition;

        /// <summary>
        /// The mouse position at a <see cref="MouseDownEvent"/> in local space.
        /// </summary>
        public Vector2 MouseDownPosition => ToLocalSpace(ScreenSpaceMouseDownPosition);

        protected MouseButtonEvent(InputState state, Vector2 screenSpaceCurrentMousePosition, MouseButton button, Vector2? screenSpaceMouseDownPosition)
            : base(state, screenSpaceCurrentMousePosition)
        {
            Button = button;
            ScreenSpaceMouseDownPosition = screenSpaceMouseDownPosition ?? ScreenSpaceCurrentMousePosition;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Button})";
    }
}
