// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.Event
{
    public class ScrollEvent : UIEvent
    {
        public Vector2 ScrollDelta;
        public bool IsPrecise;

        public ScrollEvent(InputState state, Vector2 scrollDelta)
            : base(state)
        {
            ScrollDelta = scrollDelta;
        }
    }
}
