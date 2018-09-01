// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// MarkdownQuoteBlock :
    /// > NOTE: This document does not describe the `liquid` language.
    /// </summary>
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
                textFlowContainer = CreateMarkdownTextFlowContainer()
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
                textFlowContainer.ParagraphBlock = paragraphBlock;
        }

        protected virtual Drawable CreateBackground()
        {
            return new Box
            {
                Colour = Color4.Gray,
                Width = 5,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                RelativeSizeAxes = Axes.Y
            };
        }

        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer()
        {
            return new MarkdownTextFlowContainer
            {
                Margin = new MarginPadding { Left = 20 }
            };
        }
    }
}
