// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    public class MarkdownLinkText : CompositeDrawable, IHasTooltip, IMarkdownTextComponent
    {
        public LocalisableString TooltipText => Url;

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; }

        [Resolved]
        private GameHost host { get; set; }

        private readonly string text;

        protected readonly string Url;

        public MarkdownLinkText(string text, string url)
        {
            this.text = text;
            Url = url;

            AutoSizeAxes = Axes.Both;
        }

        public MarkdownLinkText(string text, LinkInline linkInline)
            : this(text, linkInline.Url ?? string.Empty)
        {
        }

        public MarkdownLinkText(AutolinkInline autolinkInline)
            : this(autolinkInline.Url, autolinkInline.Url)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            SpriteText spriteText;
            InternalChildren = new Drawable[]
            {
                new ClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = spriteText = CreateSpriteText(),
                    Action = OnLinkPressed,
                }
            };

            spriteText.Text = text;
        }

        protected virtual void OnLinkPressed() => host.OpenUrlExternally(Url);

        public virtual SpriteText CreateSpriteText()
        {
            var spriteText = parentTextComponent.CreateSpriteText();
            spriteText.Colour = Color4.DodgerBlue;
            return spriteText;
        }
    }
}
