// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event represeting the end of a mouse drag.
    /// Triggered when the mouse button used for dragging is released.
    /// </summary>
    public class DragEndEvent : MouseButtonEvent
    {
        public DragEndEvent(InputState state, MouseButton button, Vector2? screenSpaceMouseDownPosition = null)
            : base(state, button, screenSpaceMouseDownPosition)
        {
        }
    }
}
