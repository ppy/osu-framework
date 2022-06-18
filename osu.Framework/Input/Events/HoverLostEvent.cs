// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing the end of mouse hover.
    /// Triggered when mouse cursor moved out of a drawable.
    /// </summary>
    public class HoverLostEvent : MouseEvent
    {
        public HoverLostEvent(InputState state)
            : base(state)
        {
        }
    }
}
