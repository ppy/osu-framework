// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing a mouse hover.
    /// Triggered when mouse cursor is moved onto a drawable.
    /// </summary>
    public class HoverEvent : MouseEvent
    {
        public HoverEvent(InputState state)
            : base(state)
        {
        }
    }
}
