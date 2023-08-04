// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An overlay container that eagerly holds keyboard focus.
    /// </summary>
    public abstract partial class FocusedOverlayContainer : OverlayContainer
    {
        public override bool RequestsFocus => State.Value == Visibility.Visible;

        public override bool AcceptsFocus => State.Value == Visibility.Visible;

        protected override void UpdateState(ValueChangedEvent<Visibility> state)
        {
            base.UpdateState(state);

            switch (state.NewValue)
            {
                case Visibility.Hidden:
                    if (HasFocus)
                        GetContainingInputManager().ChangeFocus(null);
                    break;

                case Visibility.Visible:
                    Schedule(() => GetContainingInputManager().TriggerFocusContention(this));
                    break;
            }
        }
    }
}
