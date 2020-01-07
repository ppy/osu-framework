// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Represents a mouse click.
    /// </summary>
    public class ClickEvent : MouseButtonEvent
    {
        public ClickEvent(InputState state, Vector2 screenSpaceCurrentMousePosition, MouseButton button, Vector2? screenSpaceMouseDownPosition = null)
            : base(state, screenSpaceCurrentMousePosition, button, screenSpaceMouseDownPosition)
        {
        }
    }
}
