// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represents events of a mouse button.
    /// </summary>
    public abstract class MouseButtonEvent : MouseEvent
    {
        public readonly MouseButton Button;
        public readonly Vector2 ScreenSpaceMouseDownPosition;

        public Vector2 MouseDownPosition => ToLocalSpace(ScreenSpaceMouseDownPosition);

        protected MouseButtonEvent(InputState state, MouseButton button, Vector2? screenSpaceMouseDownPosition)
            : base(state)
        {
            Button = button;
            ScreenSpaceMouseDownPosition = screenSpaceMouseDownPosition ?? ScreenSpaceMousePosition;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Button})";
    }
}
