// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using OpenTK;

namespace osu.Framework.Event
{
    public class MousePositionChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The last mouse position.
        /// </summary>
        public readonly Vector2 LastPosition;

        public MousePositionChangeEvent(InputState state, IInput input, Vector2 lastPosition)
            : base(state, input)
        {
            LastPosition = lastPosition;
        }
    }
}
