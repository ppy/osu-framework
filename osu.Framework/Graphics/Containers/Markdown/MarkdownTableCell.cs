// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a cell of a <see cref="MarkdownTable"/>.
    /// </summary>
    /// <code>
    /// |  cell 1   |  cell 2   |
    /// </code>
    public class MarkdownTableCell : CompositeDrawable
    {
        public float ContentWidth => textFlowContainer.TotalTextWidth;
        public float ContentHeight => textFlowContainer.DrawHeight;

        private readonly MarkdownTextFlowContainer textFlowContainer;

        public MarkdownTableCell(TableCell cell, TableColumnDefinition definition, bool isHeading)
        {
            RelativeSizeAxes = Axes.Both;

            BorderThickness = 1.8f;
            BorderColour = Color4.White;
            Masking = true;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true
                },
                textFlowContainer = CreateTextFlowContainer()
            };

            textFlowContainer.Anchor = Anchor.CentreLeft;
            textFlowContainer.Origin = Anchor.CentreLeft;

            if (cell.LastChild is ParagraphBlock paragraphBlock)
                textFlowContainer.AddInlineText(paragraphBlock.Inline);

            switch (definition.Alignment)
            {
                case TableColumnAlign.Center:
                    textFlowContainer.TextAnchor = Anchor.Centre;
                    break;
                case TableColumnAlign.Right:
                    textFlowContainer.TextAnchor = Anchor.CentreRight;
                    break;
                default:
                    textFlowContainer.TextAnchor = Anchor.CentreLeft;
                    break;
            }
        }

        protected virtual MarkdownTextFlowContainer CreateTextFlowContainer() => new MarkdownTextFlowContainer { Padding = new MarginPadding(10) };
    }
}
