// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public enum DropDownMenuItemState
    {
        NotSelected,
        Selected,
    }

    public abstract class DropDownMenuItem<T> : ClickableContainer, IStateful<DropDownMenuItemState>
    {
        public int Index;
        public int PositionIndex;
        public readonly string DisplayText;
        public readonly T Value;
        public virtual bool CanSelect { get; set; } = true;

        private bool selected;

        public bool IsSelected
        {
            get
            {
                if (!CanSelect)
                    return false;
                return selected;
            }
            set
            {
                selected = value;
                OnSelectChange();
            }
        }

        public DropDownMenuItemState State
        {
            get
            {
                return IsSelected ? DropDownMenuItemState.Selected : DropDownMenuItemState.NotSelected;
            }

            set
            {
                IsSelected = (value == DropDownMenuItemState.Selected);
            }
        }

        protected Box Background;
        protected Container Foreground;
        
        private Color4 backgroundColour = Color4.DarkSlateGray;
        protected Color4 BackgroundColour
        {
            get { return backgroundColour; }
            set
            {
                backgroundColour = value;
                FormatBackground();
            }
        }
        protected Color4 BackgroundColourHover { get; set; } = Color4.DarkGray;
        protected Color4 BackgroundColourSelected { get; set; } = Color4.SlateGray;

        protected override Container<Drawable> Content => Foreground;

        public DropDownMenuItem(string text, T value)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            DisplayText = text;
            Value = value;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                Foreground = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
            };
        }

        protected virtual void OnSelectChange()
        {
            if (!IsLoaded)
                return;
            FormatBackground();
        }

        protected virtual void FormatBackground(bool hover = false)
        {
            Background.FadeColour(hover ? BackgroundColourHover : (IsSelected ? BackgroundColourSelected : BackgroundColour), 0);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Background.Colour = IsSelected ? BackgroundColourSelected : BackgroundColour;
        }

        protected override bool OnHover(InputState state)
        {
            FormatBackground(true);
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            base.OnHover(state);
            FormatBackground();
        }
    }
}