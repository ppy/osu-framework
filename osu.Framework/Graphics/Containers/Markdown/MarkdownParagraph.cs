// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a paragraph.
    /// </summary>
    public class MarkdownParagraph : MarkdownTextFlowContainer
    {
        public MarkdownParagraph(ParagraphBlock paragraphBlock, int level)
        {
            AddInlineText(paragraphBlock.Inline);
        }
    }
}
