// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes a relative change of mouse scroll.
    /// Pointing devices such as mice provide relative scroll input.
    /// </summary>
    public class MouseScrollRelativeInput : IInput
    {
        /// <summary>
        /// The change in scroll. This is added to the current scroll.
        /// </summary>
        public Vector2 Delta;

        /// <summary>
        /// Whether the change occurred as the result of a precise scroll.
        /// </summary>
        public bool IsPrecise;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;
            if (Delta != Vector2.Zero)
            {
                mouse.LastScroll = mouse.Scroll;
                mouse.Scroll += Delta;
                mouse.HasPreciseScroll = IsPrecise;
                handler.HandleMouseScrollChange(state);
            }
        }
    }
}
