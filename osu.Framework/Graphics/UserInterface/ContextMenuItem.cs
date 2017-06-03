// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuItem : MenuItem
    {
        private const int transition_length = 200;
        private const int margin_horizontal = 15;
        public const int MARGIN_VERTICAL = 5;

        private readonly SpriteText text;
        private readonly SpriteText textBold;

        private readonly Container contentContainer;

        private ContextMenuType type;

        private bool enabled = true;
        public new bool Enabled
        {
            set
            {
                enabled = value;

                if(IsLoaded)
                    updateTextColour();
            }
            get { return enabled; }
        }


        public new float DrawWidth
        {
            get
            {
                return contentContainer.DrawWidth;
            }
        }

        public ContextMenuItem(string title, ContextMenuType type = ContextMenuType.Standard)
        {
            this.type = type;

            Children = new Drawable[]
            {
                contentContainer = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Children = new Drawable[]
                    {
                        text = new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            TextSize = 17,
                            Text = title,
                            Margin = new MarginPadding{ Horizontal = margin_horizontal, Vertical = MARGIN_VERTICAL },
                        },
                        textBold = new SpriteText
                        {
                            AlwaysPresent = true,
                            Alpha = 0,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            TextSize = 17,
                            Text = title,
                            Font = @"Exo2.0-Bold",
                            Margin = new MarginPadding{ Horizontal = margin_horizontal, Vertical = MARGIN_VERTICAL },
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            BackgroundColour = Color4.Transparent;
            BackgroundColourHover = Color4.Blue;

            updateTextColour();
        }

        private void updateTextColour()
        {
            if (Enabled)
            {
                switch (type)
                {
                    case ContextMenuType.Standard:
                        text.Colour = textBold.Colour = Color4.White;
                        break;
                    case ContextMenuType.Destructive:
                        text.Colour = textBold.Colour = Color4.Red;
                        break;
                    case ContextMenuType.Highlighted:
                        text.Colour = textBold.Colour = Color4.Yellow;
                        break;
                }
            }
            else
            {
                text.Colour = textBold.Colour = Color4.Gray;
            }
        }

        protected override bool OnHover(InputState state)
        {
            textBold.FadeIn(transition_length, EasingTypes.OutQuint);
            text.FadeOut(transition_length, EasingTypes.OutQuint);
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            textBold.FadeOut(transition_length, EasingTypes.OutQuint);
            text.FadeIn(transition_length, EasingTypes.OutQuint);
            base.OnHoverLost(state);
        }

        protected override bool OnClick(InputState state)
        {
            return enabled ? base.OnClick(state) : false;
        }
    }
}
