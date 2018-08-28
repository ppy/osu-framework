// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using Markdig.Syntax;
using OpenTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// MarkdownHeading :
    /// #Heading1
    /// ##Heading2
    /// ###Heading3
    /// ###3Heading4
    /// </summary>
    public class MarkdownHeading : CompositeDrawable
    {
        public MarkdownHeading(HeadingBlock headingBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            InternalChildren = new Drawable[]
            {
                textFlowContainer = CreateMarkdownTextFlowContainer()
            };

            var level = headingBlock.Level;
            textFlowContainer.Scale = new Vector2(GetFontSizeByLevel(level));
            textFlowContainer.AddInlineText(headingBlock.Inline);
        }

        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer() =>
            new MarkdownTextFlowContainer();

        protected float GetFontSizeByLevel(int level)
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
