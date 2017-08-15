// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An overlay container that eagerly holds keyboard focus.
    /// </summary>
    public abstract class FocusedOverlayContainer : OverlayContainer
    {
        protected InputManager InputManager;

        public override bool RequestsFocus => State == Visibility.Visible;

        public override bool AcceptsFocus => true;

        protected override void OnFocusLost(InputState state)
        {
            if (state.Keyboard.Keys.Contains(Key.Escape))
                Hide();
            base.OnFocusLost(state);
        }

        protected override void PopIn()
        {
            Schedule(() => GetContainingInputManager().TriggerFocusContention());
        }

        protected override void PopOut()
        {
            if (HasFocus)
                GetContainingInputManager().ChangeFocus(null);
        }
    }
}
