// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropDownMenuItem : Container
    {
        protected DropDownMenu ParentMenu;

        public int Index;
        public int PositionIndex;
        public object Item;

        public bool IsSelected => ParentMenu?.SelectedIndex == Index;

        public float ExpectedHeight =>
            Label.TextSize + Label.Margin.TotalVertical + Label.Padding.TotalVertical + Padding.TotalVertical;
        public float ExpectedPositionY => ExpectedHeight * PositionIndex;

        protected Box Background;
        protected virtual Color4 BackgroundColour => Color4.DarkSlateGray;
        protected virtual Color4 BackgroundColourHover => Color4.DarkGray;
        protected Container Foreground;
        protected SpriteText Label;
        protected Container Caret;
        protected virtual float CaretSpacing => 15;

        public DropDownMenuItem(DropDownMenu parent)
        {
            ParentMenu = parent;

            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                Foreground = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        Caret = new SpriteText(),
                        Label = new SpriteText(),
                    }
                },
            };
        }

        protected virtual void FormatCaret()
        {
            (Caret as SpriteText).Text = IsSelected ? @">>" : @">";
        }

        protected virtual void FormatLabel()
        {
            Label.Text = Item.ToString();
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            Background.Colour = BackgroundColour;
            RelativeSizeAxes = Axes.X;
            Width = 1;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            Label.MoveTo(new Vector2(CaretSpacing, 0));
            FormatCaret();
            FormatLabel();
        }

        protected override bool OnClick(InputState state)
        {
            if (Index >= 0)
                ParentMenu.SelectedIndex = Index;
            return false;
        }

        protected override bool OnHover(InputState state)
        {
            Background.Colour = BackgroundColourHover;
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            Background.Colour = BackgroundColour;
            base.OnHover(state);
        }
    }
}
