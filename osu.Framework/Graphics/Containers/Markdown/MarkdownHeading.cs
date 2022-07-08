// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;

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
    public class MarkdownHeading : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly HeadingBlock headingBlock;

        public MarkdownHeading(HeadingBlock headingBlock)
        {
            this.headingBlock = headingBlock;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlow;
            InternalChild = textFlow = CreateTextFlow();

            textFlow.AddInlineText(headingBlock.Inline);
        }

        public virtual MarkdownTextFlowContainer CreateTextFlow() => new MarkdownHeadingTextFlowContainer
        {
            FontSize = GetFontSizeByLevel(headingBlock.Level),
        };

        protected virtual float GetFontSizeByLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 54;

                case 2:
                    return 40;

                case 3:
                    return 30;

                case 4:
                    return 26;

                default:
                    return 20;
            }
        }

        private class MarkdownHeadingTextFlowContainer : MarkdownTextFlowContainer
        {
            public float FontSize;

            protected internal override SpriteText CreateSpriteText()
                => base.CreateSpriteText().With(t => t.Font = t.Font.With(size: FontSize));
        }
    }
}
