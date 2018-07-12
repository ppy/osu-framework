// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Event
{
    public class DragEndEvent : MouseButtonEvent
    {
        public DragEndEvent(InputState state, MouseButton button)
            : base(state, button)
        {
        }
    }
}
