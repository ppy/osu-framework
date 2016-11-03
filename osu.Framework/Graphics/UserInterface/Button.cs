﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class Button : ClickableContainer
    {
        private Box box;
        private SpriteText spriteText;

        private string text;

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                if (spriteText != null) spriteText.Text = value;
            }
        }

        private Color4 backgroundColour;

        public new Color4 Colour
        {
            get { return backgroundColour; }
            set
            {
                backgroundColour = value;
                if (box != null) box.Colour = value;
            }
        }
        
        public Button()
        {
            Height = 25;
            Colour = new Color4(14, 132, 165, 255);
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            Add(box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = backgroundColour
            });

            Add(spriteText = new SpriteText
            {
                Text = text,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
            });
        }

        protected override bool OnClick(InputState state)
        {
            var flash = new Box
            {
                RelativeSizeAxes = Axes.Both
            };

            Add(flash);

            flash.Colour = backgroundColour;
            flash.Additive = true;
            flash.Alpha = 0.3f;
            flash.FadeOutFromOne(200);
            flash.Expire();

            return base.OnClick(state);
        }
    }
}
