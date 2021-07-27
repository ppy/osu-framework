// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input
{
    public class TabletAuxiliaryButtonEventManager : ButtonEventManager<TabletAuxiliaryButton>
    {
        public TabletAuxiliaryButtonEventManager(TabletAuxiliaryButton button)
            : base(button)
        {
        }

        protected override Drawable HandleButtonDown(InputState state, ReadOnlyInputQueue targets)
        {
            var tabletAuxiliaryButtonPressEvent = new TabletAuxiliaryButtonPressEvent(state, Button);

            return PropagateButtonEvent(targets, tabletAuxiliaryButtonPressEvent);
        }

        protected override void HandleButtonUp(InputState state, List<Drawable> targets)
        {
            if (targets == null)
                return;

            PropagateButtonEvent(targets, new TabletAuxiliaryButtonReleaseEvent(state, Button));
        }
    }
}
