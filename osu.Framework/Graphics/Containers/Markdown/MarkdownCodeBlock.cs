// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises an indented/fenced code block.
    /// </summary>
    /// <code>
    /// ```
    /// code1
    /// code2
    /// code3
    /// ```
    /// </code>
    /// <code>
    ///
    ///     code1
    ///     code2
    ///     code3
    ///
    /// </code>
    public partial class MarkdownCodeBlock : CompositeDrawable, IMarkdownTextFlowComponent
    {
        private readonly CodeBlock codeBlock;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; } = null!;

        public MarkdownCodeBlock(CodeBlock codeBlock)
        {
            this.codeBlock = codeBlock;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlowContainer;
            InternalChildren = new[]
            {
                CreateBackground(),
                textFlowContainer = CreateTextFlow(),
            };

            // Markdig sometimes appends empty lines to the processed block, only add original lines to the container
            for (int i = 0; i < codeBlock.Lines.Count; i++)
                textFlowContainer.AddParagraph(codeBlock.Lines.Lines[i].ToString());
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
