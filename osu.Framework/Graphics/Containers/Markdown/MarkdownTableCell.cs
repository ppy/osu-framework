// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownTableCell : CompositeDrawable
    {
        public readonly MarkdownTextFlowContainer TextFlowContainer;

        public MarkdownTableCell(TableCell cell, TableColumnDefinition definition, int rowNumber,int columnNumber)
        {
            RelativeSizeAxes = Axes.Both;
            BorderThickness = 1.8f;
            BorderColour = Color4.White;
            Masking = true;

            InternalChildren = new []
            {
                CreateBackground(rowNumber,columnNumber),
                TextFlowContainer = CreateMarkdownTextFlowContainer()
            };

            foreach (var block in cell)
            {
                var single = (ParagraphBlock)block;
                TextFlowContainer.ParagraphBlock = single;
            }

            switch (definition.Alignment)
            {
                case TableColumnAlign.Center:
                    TextFlowContainer.TextAnchor = Anchor.TopCentre;
                    break;

                case TableColumnAlign.Right:
                    TextFlowContainer.TextAnchor = Anchor.TopRight;
                    break;

                default:
                    TextFlowContainer.TextAnchor = Anchor.TopLeft;
                    break;
            }
        }

        protected virtual Drawable CreateBackground(int rowNumber, int columnNumber)
        {
            var backgroundColor = rowNumber % 2 != 0 ? Color4.White : Color4.LightGray;
            var backgroundAlpha = 0.3f;
            if (rowNumber == 0)
            {
                backgroundColor = Color4.White;
                backgroundAlpha = 0.4f;
            }

            return new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = backgroundColor,
                Alpha = backgroundAlpha
            };
        }

        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer()
        {
            return new MarkdownTextFlowContainer
            {
                Padding = new MarginPadding { Left = 5, Right = 5, Top = 5, Bottom = 0 }
            };
        }
    }
}
