// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DropdownMenuItem<T> : MenuItem
    {
        public readonly T Value;
        private bool selected;

        public bool IsSelected
        {
            get
            {
                return Enabled.Value && selected;
            }
            set
            {
                if (selected == value) return;

                selected = value;
                OnSelectChange();
            }
        }

        private Color4 backgroundColourSelected = Color4.SlateGray;

        public Color4 BackgroundColourSelected
        {
            get { return backgroundColourSelected; }
            set
            {
                backgroundColourSelected = value;
                FormatBackground();
            }
        }

        private Color4 foregroundColourSelected = Color4.White;

        public Color4 ForegroundColourSelected
        {
            get { return foregroundColourSelected; }
            set
            {
                foregroundColourSelected = value;
                FormatForeground();
            }
        }

        protected override Container<Drawable> Content => Foreground;

        protected DropdownMenuItem(string text, T value)
        {
            if (value == null)
                throw new ArgumentNullException($"{nameof(DropdownMenuItem<T>)} does not support null value!");
            Text = text;
            Value = value;
        }

        protected virtual void OnSelectChange()
        {
            if (!IsLoaded)
                return;
            FormatBackground();
            FormatForeground();
        }

        protected override void FormatBackground(bool hover = false)
        {
            Background.FadeColour(hover ? BackgroundColourHover : (IsSelected ? BackgroundColourSelected : BackgroundColour));
        }

        protected override void FormatForeground(bool hover = false)
        {
            Foreground.FadeColour(hover ? ForegroundColourHover : (IsSelected ? ForegroundColourSelected : ForegroundColour));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Background.Colour = IsSelected ? BackgroundColourSelected : BackgroundColour;
        }

        protected override bool OnHover(InputState state)
        {
            FormatBackground(true);
            FormatForeground(true);
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            base.OnHover(state);
            FormatBackground();
            FormatForeground();
        }
    }
}
