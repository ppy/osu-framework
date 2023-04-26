// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Extensions.Footnotes;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;

namespace osu.Framework.Graphics.Containers.Markdown.Footnotes
{
    /// <summary>
    /// Visualises an in-text <see cref="FootnoteLink"/> which references a <see cref="Footnote"/>.
    /// </summary>
    public partial class MarkdownFootnoteLink : CompositeDrawable
    {
        private readonly FootnoteLink footnoteLink;

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; } = null!;

        public MarkdownFootnoteLink(FootnoteLink footnoteLink)
        {
            this.footnoteLink = footnoteLink;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var spriteText = parentTextComponent.CreateSpriteText();

            AutoSizeAxes = Axes.Both;
            InternalChild = spriteText.With(t =>
            {
                float baseSize = t.Font.Size;
                t.Font = t.Font.With(size: baseSize * 0.58f);
                t.Margin = new MarginPadding { Bottom = 0.33f * baseSize };
                t.Text = footnoteLink.Index.ToLocalisableString();
            });
        }
    }
}
