// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Input;
using OpenTK.Input;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : Container, IStateful<Visibility>
    {
        /// <summary>
        /// Whether we should automatically hide on the user pressing escape.
        /// </summary>
        protected virtual bool HideOnEscape => true;

        /// <summary>
        /// Whether we should block any mouse input from interacting with things behind us.
        /// </summary>
        protected virtual bool BlockPassThroughInput => true;

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

        public void ToggleVisibility() => State = State == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

        public override bool HandleInput => State == Visibility.Visible;

        protected override bool OnHover(InputState state) => BlockPassThroughInput;

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => BlockPassThroughInput;

        protected override bool OnClick(InputState state) => BlockPassThroughInput;

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Escape:
                    if (State == Visibility.Hidden || !HideOnEscape) return false;
                    Hide();
                    return true;
            }

            return base.OnKeyDown(state, args);
        }
    }

    public enum Visibility
    {
        Hidden,
        Visible
    }
}
