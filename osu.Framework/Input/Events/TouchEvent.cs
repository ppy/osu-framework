// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represents a touch event.
    /// </summary>
    public class TouchEvent : UIEvent
    {
        /// <summary>
        /// The touch that fired this event.
        /// </summary>
        public readonly Touch ScreenSpaceTouch;

        /// <summary>
        /// The current touch position in the screen space.
        /// </summary>
        public Vector2 ScreenSpaceTouchPosition => ScreenSpaceTouch.Position;

        /// <summary>
        /// The current touch position in local space.
        /// </summary>
        public Vector2 TouchPosition => ToLocalSpace(ScreenSpaceTouchPosition);

        /// <summary>
        /// The touch position at a <see cref="TouchDownEvent"/> in the screen space.
        /// </summary>
        public readonly Vector2 ScreenSpaceTouchDownPosition;

        /// <summary>
        /// The touch position at a <see cref="TouchDownEvent"/> in local space.
        /// </summary>
        public Vector2 TouchDownPosition => ToLocalSpace(ScreenSpaceTouchDownPosition);

        /// <summary>
        /// Whether a touch is active.
        /// </summary>
        /// <param name="touch">The touch to check for.</param>
        public bool IsActive(Touch touch) => CurrentState.Touch.IsActive(touch.Source);

        public TouchEvent(InputState state, Touch touch, Vector2? screenSpaceTouchDownPosition = null)
            : base(state)
        {
            ScreenSpaceTouch = touch;
            ScreenSpaceTouchDownPosition = screenSpaceTouchDownPosition ?? ScreenSpaceTouchPosition;
        }

        public override string ToString() => $"{GetType().ReadableName()}({ScreenSpaceTouch.Source})";
    }
}
