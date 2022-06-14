// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class TabletPenButtonInput : ButtonInput<TabletPenButton>
    {
        public TabletPenButtonInput(IEnumerable<ButtonInputEntry<TabletPenButton>> entries)
            : base(entries)
        {
        }

        public TabletPenButtonInput(TabletPenButton button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public TabletPenButtonInput(ButtonStates<TabletPenButton> current, ButtonStates<TabletPenButton> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<TabletPenButton> GetButtonStates(InputState state) => state.Tablet.PenButtons;
    }
}
