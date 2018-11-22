// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a list.
    /// </summary>
    /// <code>
    /// * item 1
    /// * item 2
    /// </code>
    public class MarkdownList : FillFlowContainer
    {
        public MarkdownList(ListBlock listBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Direction = FillDirection.Vertical;

            Spacing = new Vector2(10, 10);
            Padding = new MarginPadding { Left = 25, Right = 5 };
        }
    }
}
