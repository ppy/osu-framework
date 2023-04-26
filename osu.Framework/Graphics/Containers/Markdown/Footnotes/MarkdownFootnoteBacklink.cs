// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Extensions.Footnotes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown.Footnotes
{
    /// <summary>
    /// Visualises a backlink from a <see cref="Footnote"/> to the <see cref="FootnoteLink"/> that referenced it.
    /// These backlinks are usually placed in <see cref="FootnoteGroup"/>s.
    /// </summary>
    public partial class MarkdownFootnoteBacklink : CompositeDrawable
    {
        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            float fontSize = parentTextComponent.CreateSpriteText().Font.Size;

            AutoSizeAxes = Axes.X;
            Height = fontSize;

            InternalChild = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Margin = new MarginPadding { Left = 5 },
                Size = new Vector2(fontSize / 2),
                Icon = FontAwesome.Solid.ArrowUp,
                Colour = Color4.DodgerBlue
            };
        }
    }
}
