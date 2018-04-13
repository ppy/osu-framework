// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An overlay container that eagerly holds keyboard focus.
    /// </summary>
    public abstract class FocusedOverlayContainer : OverlayContainer
    {
        public override bool RequestsFocus => State == Visibility.Visible;

        public override bool AcceptsFocus => true;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (HasFocus && State == Visibility.Visible && !args.Repeat)
            {
                switch (args.Key)
                {
                    case Key.Escape:
                        Hide();
                        return true;
                }
            }

            return base.OnKeyDown(state, args);
        }

        protected override void PopIn()
        {
            Schedule(() => GetContainingInputManager().TriggerFocusContention(this));
        }

        protected override void PopOut()
        {
            if (HasFocus)
                GetContainingInputManager().ChangeFocus(null);
        }
    }
}
