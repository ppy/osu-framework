// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a horizontal separator.
    /// </summary>
    /// <code>
    /// ---
    /// </code>
    public class MarkdownSeparator : CompositeDrawable
    {
        public MarkdownSeparator(ThematicBreakBlock thematicBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = CreateSeparator();
        }

        protected virtual Drawable CreateSeparator() => new Box
        {
            RelativeSizeAxes = Axes.X,
            Height = 1,
            Colour = Color4.Gray,
        };
    }
}
