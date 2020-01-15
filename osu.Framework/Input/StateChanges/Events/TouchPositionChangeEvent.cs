﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// The last position of <see cref="Touch"/>.
        /// </summary>
        public readonly Vector2 LastPosition;

        public TouchPositionChangeEvent(InputState state, IInput input, MouseButton source, Vector2 lastPosition)
            : base(state, input)
        {
            Source = source;
            LastPosition = lastPosition;
        }
    }
}
