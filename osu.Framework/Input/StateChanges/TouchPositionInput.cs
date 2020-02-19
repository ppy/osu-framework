// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an absolute change of a provided touch source's position.
    /// Any provided touch source should always be in the range <see cref="MouseButton.Touch1"/>-<see cref="MouseButton.Touch10"/>.
    /// </summary>
    public class TouchPositionInput : IInput
    {
        /// <summary>
        /// The touch structure providing the new position to move to.
        /// </summary>
        public readonly Touch Touch;

        /// <summary>
        /// Constructs a <see cref="TouchPositionInput"/> from a <see cref="Touch"/> structure.
        /// </summary>
        /// <param name="touch">The <see cref="Touch"/> to construct from.</param>
        public TouchPositionInput(Touch touch)
        {
            if (touch.Source < MouseButton.Touch1 || touch.Source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid source provided within a touch: {touch.Source}", nameof(touch));

            Touch = touch;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            Vector2? lastPosition = state.Touch.GetTouchPosition(Touch.Source);
            if (lastPosition == Touch.Position)
                return;

            state.Touch.TouchPositions[Touch.Source - MouseButton.Touch1] = Touch.Position;
            handler.HandleInputStateChange(new TouchPositionChangeEvent(state, this, new Touch(Touch.Source, lastPosition ?? Touch.Position)));
        }
    }
}
