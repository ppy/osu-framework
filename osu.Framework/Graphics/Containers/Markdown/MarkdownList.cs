// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
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
