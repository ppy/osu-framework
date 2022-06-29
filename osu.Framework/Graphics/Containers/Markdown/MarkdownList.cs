// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        public MarkdownList()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Direction = FillDirection.Vertical;

            Spacing = new Vector2(10, 10);
            Padding = new MarginPadding { Left = 25, Right = 5 };
        }
    }
}
