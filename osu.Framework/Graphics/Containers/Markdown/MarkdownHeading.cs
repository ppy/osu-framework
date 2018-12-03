// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using Markdig.Syntax;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a heading.
    /// </summary>
    /// <code>
    /// # H1
    /// ## H2
    /// ### H3
    /// </code>
    public class MarkdownHeading : CompositeDrawable
    {
        public MarkdownHeading(HeadingBlock headingBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            InternalChild = textFlowContainer = CreateTextFlowContainer();

            var level = headingBlock.Level;
            textFlowContainer.Scale = new Vector2(GetFontSizeByLevel(level));
            textFlowContainer.AddInlineText(headingBlock.Inline);
        }

        protected virtual MarkdownTextFlowContainer CreateTextFlowContainer() =>
            new MarkdownTextFlowContainer();

        protected virtual float GetFontSizeByLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 2.7f;
                case 2:
                    return 2;
                case 3:
                    return 1.5f;
                case 4:
                    return 1.3f;
                default:
                    return 1;
            }
        }
    }
}
