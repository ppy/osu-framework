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
    public class MarkdownFencedCodeBlock : CompositeDrawable, IMarkdownCodeFlowComponent
    {
        private readonly FencedCodeBlock fencedCodeBlock;

        [Resolved]
        private IMarkdownCodeFlowComponent parentFlowComponent { get; set; }

        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            this.fencedCodeBlock = fencedCodeBlock;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownCodeFlowContainer textFlowContainer;
            InternalChildren = new []
            {
                CreateBackground(),
                textFlowContainer = CreateCodeFlow(),
            };

            var lines = fencedCodeBlock.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                textFlowContainer.AddParagraph(lines.Lines[i].ToString());
            }
        }

        protected virtual Drawable CreateBackground() => new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = Color4.Gray,
            Alpha = 0.5f
        };

        public virtual MarkdownCodeFlowContainer CreateCodeFlow()
        {
            var textFlow = parentFlowComponent.CreateCodeFlow();
            textFlow.Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 };
            return textFlow;
        }
    }
}
