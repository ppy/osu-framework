// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a link.
    /// </summary>
    /// <code>
    /// [link text](url)
    /// </code>
    public class MarkdownLinkText : CompositeDrawable, IHasTooltip, IMarkdownTextComponent
    {
        public string TooltipText => url;

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; }

        private readonly string text;
        private readonly string url;

        public MarkdownLinkText(string text, LinkInline linkInline)
        {
            this.text = text;
            url = linkInline.Url ?? string.Empty;

            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            SpriteText spriteText;
            InternalChildren = new Drawable[]
            {
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = spriteText = CreateText(),
                    Action = () => host.OpenUrlExternally(url)
                }
            };

            spriteText.Text = text;
        }

        public virtual SpriteText CreateText()
        {
            var spriteText = parentTextComponent.CreateText();
            spriteText.Colour = Color4.DodgerBlue;
            return spriteText;
        }
    }
}
