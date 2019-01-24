// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.


using Markdig.Syntax;
using osu.Framework.Allocation;
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
    public class MarkdownHeading : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly HeadingBlock headingBlock;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; }

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

            textFlow.Scale = new Vector2(GetFontSizeByLevel(headingBlock.Level));
            textFlow.AddInlineText(headingBlock.Inline);
        }

        public virtual MarkdownTextFlowContainer CreateTextFlow() => parentFlowComponent.CreateTextFlow();

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
