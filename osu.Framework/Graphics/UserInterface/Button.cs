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
            get { return spriteText?.Text; }
            set
            {
                if (spriteText != null)
                    spriteText.Text = value;
            }
        }

        public new Color4 Colour
        {
            get { return box.Colour; }
            set { box.Colour = value; }
        }

        private Box box;
        private SpriteText spriteText;
        
        public Button()
        {
            Children = new Drawable[]
            {
                box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                spriteText = new SpriteText
                {
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

            flash.Colour = box.Colour;
            flash.BlendingMode = BlendingMode.Additive;
            flash.Alpha = 0.3f;
            flash.FadeOutFromOne(200);
            flash.Expire();

            return base.OnClick(state);
        }
    }
}
