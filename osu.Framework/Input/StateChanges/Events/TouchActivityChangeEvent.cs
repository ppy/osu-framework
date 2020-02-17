// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges.Events
{
    public class TouchActivityChangeEvent : ButtonStateChangeEvent<MouseButton>
    {
        public TouchActivityChangeEvent(InputState state, IInput input, MouseButton button, ButtonStateChangeKind kind)
            : base(state, input, button, kind)
        {
            if (button < MouseButton.Touch1 || button > MouseButton.Touch10)
                throw new ArgumentException($"Invalid touch source provided: {button}", nameof(button));
        }
    }
}
