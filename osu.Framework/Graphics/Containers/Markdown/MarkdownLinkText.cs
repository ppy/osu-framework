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
    public class MarkdownLinkText : CompositeDrawable, IHasTooltip
    {
        public string TooltipText => url;

        private readonly string url;
        private readonly ClickableContainer textContainer;

        public MarkdownLinkText(LinkInline linkInline, string text)
        {
            url = linkInline.Url ?? string.Empty;

            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                textContainer = new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = CreateText(text)
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            textContainer.Action = () => host.OpenUrlExternally(url);
        }

        protected virtual SpriteText CreateText(string text) => new SpriteText
        {
            Text = text,
            Colour = Color4.DodgerBlue
        };
    }
}
