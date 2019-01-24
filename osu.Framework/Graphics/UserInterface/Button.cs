// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    public class Button : ClickableContainer
    {
        public string Text
        {
            get => SpriteText?.Text;
            set
            {
                if (SpriteText != null)
                    SpriteText.Text = value;
            }
        }

        public Color4 BackgroundColour
        {
            get => Background.Colour;
            set => Background.FadeColour(value);
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

        protected override bool OnClick(ClickEvent e)
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

            return base.OnClick(e);
        }
    }
}
