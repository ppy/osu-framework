// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Input.States;

namespace osu.Framework.Input.StateChanges
{
    public class TabletAuxiliaryButtonInput : ButtonInput<TabletAuxiliaryButton>
    {
        public TabletAuxiliaryButtonInput(IEnumerable<ButtonInputEntry<TabletAuxiliaryButton>> entries)
            : base(entries)
        {
        }

        public TabletAuxiliaryButtonInput(TabletAuxiliaryButton button, bool isPressed)
            : base(button, isPressed)
        {
        }

        public TabletAuxiliaryButtonInput(ButtonStates<TabletAuxiliaryButton> current, ButtonStates<TabletAuxiliaryButton> previous)
            : base(current, previous)
        {
        }

        protected override ButtonStates<TabletAuxiliaryButton> GetButtonStates(InputState state) => state.Tablet.AuxiliaryButtons;
    }
}
