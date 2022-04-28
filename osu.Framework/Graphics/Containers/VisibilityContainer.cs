// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which adds a basic visibility state.
    /// </summary>
    public abstract class VisibilityContainer : Container
    {
        /// <summary>
        /// The current visibility state.
        /// </summary>
        public readonly Bindable<Visibility> State = new Bindable<Visibility>();

        private bool didInitialHide;

        /// <summary>
        /// Whether we should be in a hidden state when first displayed.
        /// Override this and set to true to *always* perform a <see cref="PopIn"/> animation even when the state is non-hidden at
        /// first display.
        /// </summary>
        protected virtual bool StartHidden => State.Value == Visibility.Hidden;

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();

            if (StartHidden)
            {
                // do this without triggering the StateChanged event, since hidden is a default.
                PopOut();
                FinishTransforms(true);
                didInitialHide = true;
            }
        }

        protected override void LoadComplete()
        {
            State.BindValueChanged(UpdateState, State.Value == Visibility.Visible || !didInitialHide);

            base.LoadComplete();
        }

        /// <summary>
        /// Show this container by setting its visibility to <see cref="Visibility.Visible"/>.
        /// </summary>
        public override void Show() => State.Value = Visibility.Visible;

        /// <summary>
        /// Hide this container by setting its visibility to <see cref="Visibility.Hidden"/>.
        /// </summary>
        public override void Hide() => State.Value = Visibility.Hidden;

        /// <summary>
        /// Toggle this container's visibility.
        /// </summary>
        public void ToggleVisibility() => State.Value = State.Value == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

        public override bool PropagateNonPositionalInputSubTree => base.PropagateNonPositionalInputSubTree && State.Value == Visibility.Visible;
        public override bool PropagatePositionalInputSubTree => base.PropagatePositionalInputSubTree && State.Value == Visibility.Visible;

        /// <summary>
        /// Implement any transition to be played when <see cref="State"/> becomes <see cref="Visibility.Visible"/>.
        /// </summary>
        protected abstract void PopIn();

        /// <summary>
        /// Implement any transition to be played when <see cref="State"/> becomes <see cref="Visibility.Hidden"/>.
        /// Will be invoked once on <see cref="LoadComplete"/> if <see cref="StartHidden"/> is set.
        /// </summary>
        protected abstract void PopOut();

        /// <summary>
        /// Called whenever <see cref="State"/> is changed.
        /// Used to update this container's elements according to the new visibility state.
        /// </summary>
        /// <param name="state">The <see cref="ValueChangedEvent{T}"/> provided by <see cref="State"/></param>
        protected virtual void UpdateState(ValueChangedEvent<Visibility> state)
        {
            switch (state.NewValue)
            {
                case Visibility.Hidden:
                    PopOut();
                    break;

                case Visibility.Visible:
                    PopIn();
                    break;
            }
        }
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
