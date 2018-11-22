// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
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
    public class MarkdownFencedCodeBlock : CompositeDrawable
    {
        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            TextFlowContainer textFlowContainer;
            InternalChildren = new []
            {
                CreateBackground(),
                textFlowContainer = CreateTextArea(),
            };

            foreach (var line in fencedCodeBlock.Lines.Lines)
                textFlowContainer.AddParagraph(line.ToString());
        }

        protected virtual Drawable CreateBackground() => new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = Color4.Gray,
            Alpha = 0.5f
        };

        protected virtual TextFlowContainer CreateTextArea() => new TextFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 }
        };
    }
}
