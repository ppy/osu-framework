// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Extensions.Tables;
using Markdig.Syntax;
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
        public readonly MarkdownTextFlowContainer TextFlowContainer;

        public MarkdownTableCell(TableCell cell, TableColumnDefinition definition, bool isHeading)
        {
            RelativeSizeAxes = Axes.Both;

            BorderThickness = 1.8f;
            BorderColour = Color4.White;
            Masking = true;

            InternalChild = TextFlowContainer = CreateTextFlowContainer();

            if (cell.LastChild is ParagraphBlock paragraphBlock)
                TextFlowContainer.ParagraphBlock = paragraphBlock;

            switch (definition.Alignment)
            {
                case TableColumnAlign.Center:
                    TextFlowContainer.TextAnchor = Anchor.Centre;
                    break;
                case TableColumnAlign.Right:
                    TextFlowContainer.TextAnchor = Anchor.CentreRight;
                    break;
                default:
                    TextFlowContainer.TextAnchor = Anchor.CentreLeft;
                    break;
            }
        }

        protected virtual MarkdownTextFlowContainer CreateTextFlowContainer() => new MarkdownTextFlowContainer
        {
            Padding = new MarginPadding { Left = 5, Right = 5, Top = 5, Bottom = 0 }
        };
    }
}
