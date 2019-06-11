// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An overlay container that eagerly holds keyboard focus.
    /// </summary>
    public abstract class FocusedOverlayContainer : OverlayContainer
    {
        public override bool RequestsFocus => State.Value == Visibility.Visible;

        public override bool AcceptsFocus => State.Value == Visibility.Visible;

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
