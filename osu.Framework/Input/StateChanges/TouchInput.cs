// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;
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
        /// The list of touch structures each providing the source and position to move to.
        /// </summary>
        public readonly IEnumerable<Touch> Touches;

        /// <summary>
        /// Whether to activate the provided <see cref="Touches"/>.
        /// </summary>
        public readonly bool Activate;

        /// <summary>
        /// Constructs a new <see cref="TouchInput"/>.
        /// </summary>
        /// <param name="touch">The <see cref="Touch"/>.</param>
        /// <param name="activate">Whether to activate the provided <param ref="touch"/>, must be true if changing position only.</param>
        public TouchInput(Touch touch, bool activate)
            : this(touch.Yield(), activate)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="TouchInput"/>.
        /// </summary>
        /// <param name="touches">The list of <see cref="Touch"/>es.</param>
        /// <param name="activate">Whether to activate the provided <param ref="touches"/>, must be true if changing position only.</param>
        public TouchInput(IEnumerable<Touch> touches, bool activate)
        {
            Touches = touches;
            Activate = activate;
        }

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var touches = state.Touch;

            foreach (var touch in Touches)
            {
                var lastPosition = touches.GetTouchPosition(touch.Source);
                touches.TouchPositions[(int)touch.Source] = touch.Position;

                bool activityChanged = touches.ActiveSources.SetPressed(touch.Source, Activate);
                bool positionChanged = lastPosition != null && touch.Position != lastPosition;

                if (activityChanged || positionChanged)
                {
                    handler.HandleInputStateChange(new TouchStateChangeEvent(state, this, touch,
                        !activityChanged ? null : Activate,
                        !positionChanged ? null : lastPosition
                    ));
                }
            }
        }
    }
}
