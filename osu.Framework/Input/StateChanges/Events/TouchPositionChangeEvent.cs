// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges.Events
{
    public class TouchPositionChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The touch source of this change event.
        /// </summary>
        public readonly MouseButton Source;

        /// <summary>
        /// The last position of the touch.
        /// </summary>
        public readonly Vector2 LastPosition;

        public TouchPositionChangeEvent(InputState state, IInput input, MouseButton source, Vector2 lastPosition)
            : base(state, input)
        {
            if (source < MouseButton.Touch1 || source > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {source}", nameof(source));

            Source = source;
            LastPosition = lastPosition;
        }
    }
}
