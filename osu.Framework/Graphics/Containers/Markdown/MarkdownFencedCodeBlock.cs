// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using Markdig.Syntax;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// MarkdownFencedCodeBlock :
    /// ```
    /// foo
    /// ```
    /// </summary>
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

            var lines = fencedCodeBlock.Lines.Lines.Take(fencedCodeBlock.Lines.Count);
            foreach (var sligneLine in lines)
            {
                var lineString = sligneLine.ToString();
                textFlowContainer.AddParagraph(lineString);
            }
        }

        protected virtual Drawable CreateBackground()
        {
            return new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.Gray,
                Alpha = 0.5f
            };
        }

        protected virtual TextFlowContainer CreateTextArea()
        {
            return new TextFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 }
            };
        }
    }
}
