// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// Whether the change came from a device supporting precision scrolling.
        /// </summary>
        /// <remarks>
        /// In cases this is true, scroll events will generally map 1:1 to user's input, rather than incrementing in large "notches" (as expected of traditional scroll wheels).
        /// </remarks>
        public bool IsPrecise;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;

            if (Delta != Vector2.Zero)
            {
                if (!IsPrecise && Delta.X == 0 && state.Keyboard.ShiftPressed)
                    Delta = new Vector2(Delta.Y, 0);

                var lastScroll = mouse.Scroll;
                mouse.Scroll += Delta;
                mouse.LastSource = this;
                handler.HandleInputStateChange(new MouseScrollChangeEvent(state, this, lastScroll, IsPrecise));
            }
        }
    }
}
