// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Extensions.Footnotes;
using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers.Markdown.Footnotes
{
    /// <summary>
    /// Visualises a single <see cref="Markdig.Extensions.Footnotes.Footnote"/> within a <see cref="FootnoteGroup"/>.
    /// </summary>
    public partial class MarkdownFootnote : CompositeDrawable, IMarkdownTextComponent, IMarkdownTextFlowComponent
    {
        public readonly Footnote Footnote;

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; } = null!;

        [Resolved]
        private IMarkdownTextFlowComponent parentFlowComponent { get; set; } = null!;

        public MarkdownFootnote(Footnote footnote)
        {
            Footnote = footnote;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MarkdownTextFlowContainer textFlow;

            InternalChildren = new Drawable[]
            {
                CreateOrderMarker(Footnote.Order),
                textFlow = CreateTextFlow()
            };

            if (Footnote.LastChild is ParagraphBlock paragraphBlock)
                textFlow.AddInlineText(paragraphBlock.Inline);
        }

        public virtual SpriteText CreateOrderMarker(int order) => CreateSpriteText().With(text =>
        {
            text.Text = order.ToLocalisableString();
        });

        public SpriteText CreateSpriteText() => parentTextComponent.CreateSpriteText();

        public virtual MarkdownTextFlowContainer CreateTextFlow() => parentFlowComponent.CreateTextFlow().With(flow =>
        {
            flow.Margin = new MarginPadding { Left = 20 };
        });
    }
}
