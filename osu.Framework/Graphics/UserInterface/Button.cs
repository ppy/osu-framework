// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public class Button : ClickableContainer
    {
        public string Text
        {
            get { return SpriteText?.Text; }
            set
            {
                if (SpriteText != null)
                    SpriteText.Text = value;
            }
        }

        public Color4 BackgroundColour
        {
            get { return Background.Colour; }
            set { Background.FadeColour(value); }
        }

        protected override Container<Drawable> Content => content;

        private readonly Container content;

        protected Box Background;
        protected SpriteText SpriteText;

        public Button()
        {
            AddInternal(content = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    Background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    SpriteText = CreateText(),
                }
            });
        }

        protected virtual SpriteText CreateText() => new SpriteText
        {
            Depth = -1,
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
        };

        protected override bool OnClick(InputState state)
        {
            if (Enabled.Value)
            {
                var flash = new Box
                {
                    RelativeSizeAxes = Axes.Both
                };

                Add(flash);

                flash.Colour = Background.Colour;
                flash.Blending = BlendingMode.Additive;
                flash.FadeOutFromOne(200);
                flash.Expire();
            }

            return base.OnClick(state);
        }
    }
}
