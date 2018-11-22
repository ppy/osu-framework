// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        public MarkdownSeparator()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            InternalChild = CreateSeparator();
        }

        protected virtual Drawable CreateSeparator()
        {
            return new Box
            {
                RelativeSizeAxes = Axes.X,
                Colour = Color4.Gray,
                Height = 1,
            };
        }
    }
}
