// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a change of a touch input.
    /// </summary>
    public class TouchInput : IInput
    {
        /// <summary>
        /// The touch structure providing the source and position to move to.
        /// </summary>
        public readonly Touch Touch;

        /// <summary>
        /// Whether to activate the provided <see cref="Touch"/>.
        /// </summary>
        public readonly bool Activate;

        /// <summary>
        /// Constructs a new <see cref="TouchInput"/>.
        /// </summary>
        /// <param name="touch">The <see cref="Touch"/>.</param>
        /// <param name="activate">Whether to activate the provided <see cref="Touch"/>, must be true if changing position only.</param>
        public TouchInput(Touch touch, bool activate)
        {
            Touch = touch;
            Activate = activate;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touches = state.Touch;

            var lastPosition = touches.GetTouchPosition(Touch.Source);
            touches.TouchPositions[(int)Touch.Source] = Touch.Position;

            bool activityChanged = touches.ActiveSources.SetPressed(Touch.Source, Activate);
            var positionChanged = lastPosition != null && Touch.Position != lastPosition;

            if (activityChanged || positionChanged)
                handler.HandleInputStateChange(new TouchStateChangeEvent(state, this, Touch, !activityChanged ? (bool?)null : Activate, lastPosition));
        }
    }
}
