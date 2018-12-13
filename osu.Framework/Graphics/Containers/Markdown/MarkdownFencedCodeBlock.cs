// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a fenced code block.
    /// </summary>
    /// <code>
    /// ```
    /// code
    /// ```
    /// </code>
    public class MarkdownFencedCodeBlock : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly FencedCodeBlock fencedCodeBlock;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; }

        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            this.fencedCodeBlock = fencedCodeBlock;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlowContainer;
            InternalChildren = new []
            {
                CreateBackground(),
                textFlowContainer = CreateTextFlow(),
            };

            foreach (var line in fencedCodeBlock.Lines.Lines)
                if (line.Line > 0)
                    textFlowContainer.AddParagraph(line.ToString());
        }

        protected virtual Drawable CreateBackground() => new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = Color4.Gray,
            Alpha = 0.5f
        };

        public virtual MarkdownTextFlowContainer CreateTextFlow()
        {
            var textFlow = parentFlowComponent.CreateTextFlow();
            textFlow.Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 };
            return textFlow;
        }
    }
}
