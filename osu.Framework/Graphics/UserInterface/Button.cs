// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK.Graphics;
using osu.Framework.Allocation;

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

        public new Color4 Colour
        {
            get { return Background.Colour; }
            set { Background.Colour = value; }
        }

        protected Box Background;
        protected SpriteText SpriteText;
        
        public Button()
        {
            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                SpriteText = new SpriteText
                {
                    Depth = -1,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                }
            };
        }

        protected override bool OnClick(InputState state)
        {
            var flash = new Box
            {
                RelativeSizeAxes = Axes.Both
            };

            Add(flash);

            flash.Colour = Background.Colour;
            flash.BlendingMode = BlendingMode.Additive;
            flash.Alpha = 0.3f;
            flash.FadeOutFromOne(200);
            flash.Expire();

            return base.OnClick(state);
        }
    }
}
