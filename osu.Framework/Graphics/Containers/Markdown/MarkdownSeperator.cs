// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// MarkdownSeperator :
    /// (spacing)
    /// </summary>
    public class MarkdownSeperator : CompositeDrawable
    {
        public MarkdownSeperator()
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            InternalChild = CreateSeperator();
        }

        protected virtual Drawable CreateSeperator()
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
