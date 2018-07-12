// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.Event
{
    public class MousePositionChangeEvent : InputStateChangeEvent
    {
        /// <summary>
        /// The last mouse position.
        /// </summary>
        public Vector2 LastPosition;

        public MousePositionChangeEvent(InputState state, IInput input, Vector2 lastPosition)
            : base(state, input)
        {
            LastPosition = lastPosition;
        }
    }
}
