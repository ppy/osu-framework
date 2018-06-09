// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    public class MouseScrollAbsoluteChange : IInput
    {
        public Vector2 Scroll;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Mouse.Scroll != Scroll)
            {
                state.Mouse.Scroll = Scroll;
                handler.HandleMouseScrollChange(state);
            }
        }
    }
}
