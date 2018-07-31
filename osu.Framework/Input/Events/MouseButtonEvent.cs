// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a press of a mouse button.
    /// </summary>
    public abstract class MouseButtonEvent : MouseActionEvent
    {
        protected MouseButtonEvent(InputState state, MouseButton button, Vector2? screenSpaceMouseDownPosition)
            : base(state, button, screenSpaceMouseDownPosition)
        {
        }
    }
}
