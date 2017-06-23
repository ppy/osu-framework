// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which adds a basic visibility state.
    /// </summary>
    public abstract class VisibilityContainer : Container, IStateful<Visibility>
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

        public override void Hide() => State = Visibility.Hidden;

        public override void Show() => State = Visibility.Visible;

        public override bool HandleInput => State == Visibility.Visible;

        public event Action<VisibilityContainer, Visibility> StateChanged;

        protected abstract void PopIn();

        protected abstract void PopOut();

        public void ToggleVisibility() => State = State == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
    }
}