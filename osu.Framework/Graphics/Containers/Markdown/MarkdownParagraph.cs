// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Markdig.Syntax;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a paragraph.
    /// </summary>
    public class MarkdownParagraph : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly ParagraphBlock paragraphBlock;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; }

        public MarkdownParagraph(ParagraphBlock paragraphBlock)
        {
            this.paragraphBlock = paragraphBlock;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlow;
            InternalChild = textFlow = CreateTextFlow();

            textFlow.AddInlineText(paragraphBlock.Inline);
        }

        public virtual MarkdownTextFlowContainer CreateTextFlow() => parentFlowComponent.CreateTextFlow();
    }
}
