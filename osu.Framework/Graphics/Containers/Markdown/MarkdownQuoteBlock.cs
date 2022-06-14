// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Markdig.Syntax;
using osu.Framework.Allocation;
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
    public class MarkdownQuoteBlock : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly QuoteBlock quoteBlock;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; }

        public MarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            this.quoteBlock = quoteBlock;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlow;
            InternalChildren = new[]
            {
                CreateBackground(),
                textFlow = CreateTextFlow()
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
                textFlow.AddInlineText(paragraphBlock.Inline);
        }

        protected virtual Drawable CreateBackground() => new Box
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            RelativeSizeAxes = Axes.Y,
            Width = 5,
            Colour = Color4.Gray
        };

        public virtual MarkdownTextFlowContainer CreateTextFlow()
        {
            var textFlow = parentFlowComponent.CreateTextFlow();
            textFlow.Margin = new MarginPadding { Left = 20 };
            return textFlow;
        }
    }
}
