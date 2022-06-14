// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event representing the release of a tablet pen button.
    /// </summary>
    public class TabletPenButtonReleaseEvent : TabletPenButtonEvent
    {
        public TabletPenButtonReleaseEvent(InputState state, TabletPenButton button)
            : base(state, button)
        {
        }
    }
}
