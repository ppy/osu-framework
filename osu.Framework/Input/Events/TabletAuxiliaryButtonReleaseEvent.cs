// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing the press of a tablet auxiliary button.
    /// </summary>
    public class TabletAuxiliaryButtonReleaseEvent : TabletAuxiliaryButtonEvent
    {
        public TabletAuxiliaryButtonReleaseEvent(InputState state, TabletAuxiliaryButton button)
            : base(state, button)
        {
        }
    }
}
