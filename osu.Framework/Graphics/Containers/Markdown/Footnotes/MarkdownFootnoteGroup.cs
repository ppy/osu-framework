// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Extensions.Footnotes;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown.Footnotes
{
    /// <summary>
    /// Visualises a <see cref="FootnoteGroup"/>.
    /// </summary>
    public partial class MarkdownFootnoteGroup : FillFlowContainer
    {
        public MarkdownFootnoteGroup()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Direction = FillDirection.Vertical;

            Spacing = new Vector2(10);
        }
    }
}
