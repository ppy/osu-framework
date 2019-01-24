// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input.StateChanges
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
                var lastScroll = mouse.Scroll;
                mouse.Scroll += Delta;
                handler.HandleInputStateChange(new MouseScrollChangeEvent(state, this, lastScroll, IsPrecise));
            }
        }
    }
}
