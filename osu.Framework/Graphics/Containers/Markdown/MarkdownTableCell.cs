// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Allocation;
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
    public class MarkdownTableCell : CompositeDrawable, IMarkdownTextFlowComponent
    {
        public float ContentWidth => textFlow.TotalTextWidth;
        public float ContentHeight => textFlow.DrawHeight;

        private MarkdownTextFlowContainer textFlow;

        private readonly TableCell cell;
        private readonly TableColumnDefinition definition;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; }

        public MarkdownTableCell(TableCell cell, TableColumnDefinition definition)
        {
            this.cell = cell;
            this.definition = definition;

            RelativeSizeAxes = Axes.Both;

            BorderThickness = 1.8f;
            BorderColour = Color4.White;
            Masking = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true
                },
                textFlow = CreateTextFlow()
            };

            textFlow.Anchor = Anchor.CentreLeft;
            textFlow.Origin = Anchor.CentreLeft;

            if (cell.LastChild is ParagraphBlock paragraphBlock)
                textFlow.AddInlineText(paragraphBlock.Inline);

            switch (definition.Alignment)
            {
                case TableColumnAlign.Center:
                    textFlow.TextAnchor = Anchor.Centre;
                    break;

                case TableColumnAlign.Right:
                    textFlow.TextAnchor = Anchor.CentreRight;
                    break;

                default:
                    textFlow.TextAnchor = Anchor.CentreLeft;
                    break;
            }
        }

        public virtual MarkdownTextFlowContainer CreateTextFlow()
        {
            var flow = parentFlowComponent.CreateTextFlow();
            flow.Padding = new MarginPadding(10);
            return flow;
        }
    }
}
