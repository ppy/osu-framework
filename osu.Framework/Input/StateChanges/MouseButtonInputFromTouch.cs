// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    public class MouseButtonInputFromTouch : MouseButtonInput, ISourcedFromTouch
    {
        public MouseButtonInputFromTouch(MouseButton button, bool isPressed, TouchStateChangeEvent touchEvent)
            : base(button, isPressed)
        {
            TouchEvent = touchEvent;
        }

        public TouchStateChangeEvent TouchEvent { get; }
    }
}
