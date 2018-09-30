// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownLinkText : CompositeDrawable , IHasTooltip
    {
        public string Text
        {
            get => spriteText.Text;
            set => spriteText.Text = value;
        }

        public string Url { get; set; }

        public string TooltipText => Url;

        public ColourInfo TextColour
        {
            get => spriteText.Colour;
            set => spriteText.Colour = value;
        }

        private readonly SpriteText spriteText;

        public MarkdownLinkText()
        {
            ClickableContainer clickableContainer;
            AutoSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
                clickableContainer = new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    InternalChildren = new Drawable[]
                    {
                        spriteText = new SpriteText()
                    }
                }
            };

            clickableContainer.Action = () =>
            {
                //Show Url in browser
                Process.Start(Url);
            };
        }
    }
}
