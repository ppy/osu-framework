// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.StateChanges.Events
{
    public class MouseButtonStateChangeEvent : ButtonStateChangeEvent<MouseButton>
    {
        /// <summary>
        /// The mouse position at the button change occurrence.
        /// </summary>
        public readonly Vector2 Position;

        public MouseButtonStateChangeEvent(InputState state, IInput input, MouseButton button, ButtonStateChangeKind kind, Vector2 position)
            : base(state, input, button, kind)
        {
            Position = position;
        }
    }
}
