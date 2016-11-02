// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropDownComboBox : Container
    {
        protected DropDownMenu ParentMenu;

        protected Box Background;
        protected virtual Color4 BackgroundColour => Color4.DarkGray;
        protected virtual Color4 BackgroundColourHover => Color4.Gray;
        protected Container Foreground;
        public SpriteText Label;
        public Container Caret;

        public DropDownComboBox(DropDownMenu parent) : base()
        {
            ParentMenu = parent;

            Masking = true;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Width = 1;
            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
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

        public override void Load(BaseGame game)
        {
            base.Load(game);
            Background.Colour = BackgroundColour;
        }

        protected override bool OnClick(InputState state)
        {
            ParentMenu.Toogle();
            return true;
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
    }
}
