// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using System;
 
namespace osu.Framework.Graphics.UserInterface
{
    public class DropDownComboBox : ClickableContainer
    {
        protected Box Background;
        protected virtual Color4 BackgroundColour => Color4.DarkGray;
        protected virtual Color4 BackgroundColourHover => Color4.Gray;
        protected Container Foreground;
        protected Drawable Label;
        protected Drawable Caret;

        public Action CloseAction;

        public DropDownComboBox()
        {
            Masking = true;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Width = 1;
            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = BackgroundColour,
                },
                Foreground = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        Label = new SpriteText
                        {
                            Text = @"",
                        },
                        Caret = new SpriteText
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Text = @"+",
                        },
                    }
                },
            };
        }

        protected override bool OnFocus(InputState state)
        {
            return true;
        }

        protected override void OnFocusLost(InputState state)
        {
            CloseAction?.Invoke();
        }

        protected override bool OnHover(InputState state)
        {
            Background.Colour = BackgroundColourHover;
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            Background.Colour = BackgroundColour;
            base.OnHoverLost(state);
        }

        public void UpdateLabel(string label)
        {
            (Label as SpriteText).Text = label;
        }
    }
}