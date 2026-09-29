// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a link.
    /// </summary>
    /// <code>
    /// [link text](url)
    /// </code>
    public partial class MarkdownLinkText : CompositeDrawable, IHasTooltip, IMarkdownTextComponent, IMarkdownTextFlowComponent
    {
        public LocalisableString TooltipText => Url;

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; } = null!;

        [Resolved]
        private IMarkdownTextFlowComponent parentTextFlowComponent { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        private readonly Inline contentInline;

        private readonly bool hasBold;
        private readonly bool hasItalic;

        protected readonly string Url;

        public MarkdownLinkText(string url, Inline contentInline)
        {
            Url = url;
            this.contentInline = contentInline;

            AutoSizeAxes = Axes.Both;
        }

        public MarkdownLinkText(LinkInline linkInline)
            : this(linkInline.Url, linkInline)
        {
        }

        public MarkdownLinkText(AutolinkInline autolinkInline, bool bold = false, bool italic = false)
            : this(autolinkInline.Url, autolinkInline)
        {
            hasBold = bold;
            hasItalic = italic;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = CreateContent(),
                    Action = OnLinkPressed,
                }
            };
        }

        protected virtual MarkdownTextFlowContainer CreateContent()
        {
            var textFlow = CreateTextFlow();

            switch (contentInline)
            {
                case LinkInline linkInline:
                    textFlow.AddInlineText(linkInline);
                    break;

                case AutolinkInline autolinkInline:
                    textFlow.AddEmphasis(autolinkInline.Url, hasBold, hasItalic);
                    break;
            }

            return textFlow;
        }

        protected virtual void OnLinkPressed() => host.OpenUrlExternally(Url);

        public virtual SpriteText CreateSpriteText()
        {
            var spriteText = parentTextComponent.CreateSpriteText();
            spriteText.Colour = Color4.DodgerBlue;
            return spriteText;
        }

        public MarkdownTextFlowContainer CreateTextFlow() => parentTextFlowComponent.CreateTextFlow().With(t =>
        {
            t.RelativeSizeAxes = Axes.None;
            t.AutoSizeAxes = Axes.Both;
            t.Margin = new MarginPadding(0);
            t.Padding = new MarginPadding(0);
        });
    }
}
