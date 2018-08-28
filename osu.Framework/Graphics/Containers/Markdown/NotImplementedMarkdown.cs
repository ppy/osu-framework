// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax;
using osu.Framework.Graphics.Sprites;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a message when a <see cref="IMarkdownObject"/> doesn't have a visual implementation.
    /// </summary>
    public class NotImplementedMarkdown : CompositeDrawable
    {
        public NotImplementedMarkdown(IMarkdownObject markdownObject)
        {
            AutoSizeAxes = Axes.Y;
            InternalChildren = new SpriteText
            {
                Colour = new Color4(255, 0, 0, 255),
                TextSize = 21,
                Text = markdownObject?.GetType() + " Not implemented."
            };
        }
    }
}
