// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;

namespace osu.Framework.Graphics.Containers.Markdown
{
    public class MarkdownParagraph : MarkdownTextFlowContainer
    {
        public readonly int Level;

        public MarkdownParagraph(ParagraphBlock paragraphBlock, int level)
        {
            Level = level;

            AddInlineText(paragraphBlock.Inline);
        }
    }
}
