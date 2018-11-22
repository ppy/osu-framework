// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a quote block.
    /// </summary>
    /// <code>
    /// > Quote
    /// </code>
    public class MarkdownQuoteBlock : CompositeDrawable
    {
        public MarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            InternalChildren = new []
            {
                CreateBackground(),
                textFlowContainer = CreateTextFlowContainer()
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
                textFlowContainer.AddInlineText(paragraphBlock.Inline);
        }

        protected virtual Drawable CreateBackground() => new Box
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            RelativeSizeAxes = Axes.Y,
            Width = 5,
            Colour = Color4.Gray
        };

        protected virtual MarkdownTextFlowContainer CreateTextFlowContainer() => new MarkdownTextFlowContainer
        {
            Margin = new MarginPadding { Left = 20 }
        };
    }
}
