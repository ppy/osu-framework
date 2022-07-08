// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// The touch of this event with the screen space position.
        /// </summary>
        public readonly Touch ScreenSpaceTouch;

        /// <summary>
        /// The touch of this event with local space position.
        /// </summary>
        public Touch Touch => new Touch(ScreenSpaceTouch.Source, ToLocalSpace(ScreenSpaceTouch.Position));

        /// <summary>
        /// The touch position at the <see cref="TouchDownEvent"/> for this touch source in the screen space.
        /// </summary>
        public readonly Vector2 ScreenSpaceTouchDownPosition;

        /// <summary>
        /// The touch position at the <see cref="TouchDownEvent"/> for this touch source in local space.
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
            ScreenSpaceTouchDownPosition = screenSpaceTouchDownPosition ?? ScreenSpaceTouch.Position;
        }

        public override string ToString() => $"{GetType().ReadableName()}({ScreenSpaceTouch.Source})";
    }
}
