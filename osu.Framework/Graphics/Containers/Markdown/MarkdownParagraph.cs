// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        public MarkdownParagraph(ParagraphBlock paragraphBlock, int level)
        {
            this.paragraphBlock = paragraphBlock;
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
