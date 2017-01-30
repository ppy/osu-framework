// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : Container, IStateful<Visibility>
    {
        protected override void LoadComplete()
        {
            if (state == Visibility.Hidden)
            {
                PopOut();
                Flush(true);
            }

            base.LoadComplete();
        }
        private Visibility state;
        public Visibility State
        {
            get { return state; }
            set
            {
                if (value == state) return;
                state = value;

                switch (value)
                {
                    case Visibility.Hidden:
                        PopOut();
                        break;
                    case Visibility.Visible:
                        PopIn();
                        break;
                }

                StateChanged?.Invoke(this, state);
            }
        }

        public event Action<OverlayContainer, Visibility> StateChanged;

        protected abstract void PopIn();

        protected abstract void PopOut();

        public override void Hide() => State = Visibility.Hidden;

        public override void Show() => State = Visibility.Visible;

        public void ToggleVisibility() => State = (State == Visibility.Visible ? Visibility.Hidden : Visibility.Visible);
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
