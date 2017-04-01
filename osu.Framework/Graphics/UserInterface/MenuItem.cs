// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class MenuItem : ClickableContainer
    {
        public string Text;

        protected Box Background;
        protected Container Foreground;

        private Color4 backgroundColour = Color4.DarkSlateGray;

        public Color4 BackgroundColour
        {
            get { return backgroundColour; }
            set
            {
                backgroundColour = value;
                FormatBackground();
            }
        }

        private Color4 foregroundColour = Color4.White;

        public Color4 ForegroundColour
        {
            get { return foregroundColour; }
            set
            {
                foregroundColour = value;
                FormatForeground();
            }
        }

        private Color4 backgroundColourHover = Color4.DarkGray;

        public Color4 BackgroundColourHover
        {
            get { return backgroundColourHover; }
            set
            {
                backgroundColourHover = value;
                FormatBackground();
            }
        }

        private Color4 foregroundColourHover = Color4.White;

        public Color4 ForegroundColourHover
        {
            get { return foregroundColourHover; }
            set
            {
                foregroundColourHover = value;
                FormatForeground();
            }
        }

        protected override Container<Drawable> Content => Foreground;

        public MenuItem()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
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

        protected virtual void FormatBackground(bool hover = false)
        {
            Background.FadeColour(hover ? BackgroundColourHover : BackgroundColour);
        }

        protected virtual void FormatForeground(bool hover = false)
        {
            Foreground.FadeColour(hover ? ForegroundColourHover : ForegroundColour);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Background.Colour = BackgroundColour;
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
