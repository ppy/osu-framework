// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges.Events
{
    public class TouchStateChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The current <see cref="Touch"/> value.
        /// </summary>
        public readonly Touch Touch;

        /// <summary>
        /// Whether the <see cref="Touch"/> became active, or null if no activity change occurred.
        /// </summary>
        public readonly bool? IsActive;

        /// <summary>
        /// The last position of this <see cref="Touch"/>, or null if no position change occurred.
        /// </summary>
        public readonly Vector2? LastPosition;

        public TouchStateChangeEvent(InputState state, IInput input, Touch touch, bool? active, Vector2? lastPosition)
            : base(state, input)
        {
            Touch = touch;

            IsActive = active;
            LastPosition = lastPosition;
        }
    }
}
