// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Footnotes;
using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers.Markdown.Footnotes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Markdown text flow container.
    /// </summary>
    public partial class MarkdownTextFlowContainer : CustomizableTextContainer, IMarkdownTextComponent
    {
        public float TotalTextWidth => Padding.TotalHorizontal + Flow.FlowingChildren.Sum(x => x.BoundingBox.Size.X);
        public float MinimumTextWidth => Padding.TotalHorizontal + Flow.FlowingChildren.Select(x => x.BoundingBox.Size.X).DefaultIfEmpty(0).Max();

        [Resolved]
        private IMarkdownTextComponent parentTextComponent { get; set; } = null!;

        public MarkdownTextFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected void AddDrawable(Drawable drawable)
            => base.AddText("[" + AddPlaceholder(drawable) + "]");

        public void AddText(string text, Action<SpriteText>? creationParameters = null)
            => base.AddText(Escape(text), creationParameters);

        public ITextPart AddParagraph(string text, Action<SpriteText>? creationParameters = null)
            => base.AddParagraph(Escape(text), creationParameters);

        public void AddInlineText(ContainerInline container)
        {
            foreach (var single in container)
            {
                switch (single)
                {
                    case HtmlInline:
                    case HtmlEntityInline:
                        // Handled by the next literal
                        break;

                    case LineBreakInline lineBreak:
                        if (lineBreak.IsHard)
                            NewParagraph();
                        else
                            NewLine();
                        break;

                    case LeafInline leafInline:
                        bool hasBold = false;
                        bool hasItalic = false;

                        var parent = leafInline.Parent;

                        while (parent is Inline parentInline)
                        {
                            if (parentInline is EmphasisInline e)
                            {
                                string emphasis = new string(e.DelimiterChar, e.DelimiterCount);

                                switch (emphasis)
                                {
                                    case "*":
                                    case "_":
                                        hasItalic = true;
                                        break;

                                    case "**":
                                    case "__":
                                        hasBold = true;
                                        break;
                                }
                            }

                            parent = parent.Parent;
                        }

                        switch (leafInline)
                        {
                            case LiteralInline literal:
                                string text = literal.Content.ToString();

                                if (container.GetPrevious(literal) is HtmlInline && container.GetNext(literal) is HtmlInline)
                                    AddHtmlInLineText(text, literal);
                                else if (container.GetNext(literal) is HtmlEntityInline entityInLine)
                                    AddHtmlEntityInlineText(text, entityInLine);
                                else
                                    AddLiteralText(literal, hasBold, hasItalic);

                                break;

                            case CodeInline codeInline:
                                AddCodeInLine(codeInline, hasBold, hasItalic);
                                break;

                            case AutolinkInline autoLink:
                                AddAutoLink(autoLink, hasBold, hasItalic);
                                break;
                        }

                        break;

                    case LinkInline linkInline:
                        if (linkInline.IsImage)
                            AddImage(linkInline);
                        else
                            AddLinkText(linkInline);
                        break;

                    case CustomContainerInline customContainer:
                        AddCustomComponent(customContainer);
                        break;

                    case EmphasisInline emphasisInline:
                        AddInlineText(emphasisInline);
                        break;

                    case ContainerInline innerContainer:
                        AddInlineText(innerContainer);
                        break;

                    case FootnoteLink footnoteLink:
                        if (footnoteLink.IsBackLink)
                            AddFootnoteBacklink(footnoteLink);
                        else
                            AddFootnoteLink(footnoteLink);
                        break;

                    default:
                        AddNotImplementedInlineText(single);
                        break;
                }
            }
        }

        protected virtual void AddLiteralText(LiteralInline literalInline, bool bold = false, bool italic = false)
            => AddEmphasis(literalInline.Content.ToString(), bold, italic);

        protected virtual void AddHtmlInLineText(string text, LiteralInline literalInline)
            => AddText(text, t => t.Colour = Color4.MediumPurple);

        protected virtual void AddHtmlEntityInlineText(string text, HtmlEntityInline entityInLine)
            => AddText(text, t => t.Colour = Color4.GreenYellow);

        protected virtual void AddLinkText(LinkInline linkInline)
            => AddDrawable(new MarkdownLinkText(linkInline));

        protected virtual void AddAutoLink(AutolinkInline autolinkInline, bool bold = false, bool italic = false)
            => AddDrawable(new MarkdownLinkText(autolinkInline, bold, italic));

        protected virtual void AddCodeInLine(CodeInline codeInline, bool bold = false, bool italic = false)
            => AddText(codeInline.Content, t =>
            {
                ApplyEmphasisedCreationParameters(t, bold, italic);
                t.Colour = Color4.Orange;
            });

        protected virtual void AddImage(LinkInline linkInline)
            => AddDrawable(new MarkdownImage(linkInline.Url));

        protected virtual void AddFootnoteLink(FootnoteLink footnoteLink)
            => AddDrawable(new MarkdownFootnoteLink(footnoteLink));

        protected virtual void AddFootnoteBacklink(FootnoteLink footnoteBacklink)
            => AddDrawable(new MarkdownFootnoteBacklink());

        protected virtual void AddCustomComponent(CustomContainerInline customContainerInline)
            => AddNotImplementedInlineText(customContainerInline);

        protected virtual void AddNotImplementedInlineText(Inline inline)
            => AddText(inline.GetType() + " not implemented.", t => t.Colour = Color4.Red);

        public virtual void AddEmphasis(string text, bool hasBold, bool hasItalic)
            => AddText(text, t => ApplyEmphasisedCreationParameters(t, hasBold, hasItalic));

        protected internal override SpriteText CreateSpriteText() => parentTextComponent.CreateSpriteText();

        /// <summary>
        /// Applies emphasised creation parameters to <see cref="SpriteText"/>.
        /// </summary>
        /// <param name="spriteText">The <see cref="SpriteText"/> to be emphasised.</param>
        /// <param name="bold">Whether the text should be emboldened.</param>
        /// <param name="italic">Whether the text should be italicised.</param>
        protected virtual void ApplyEmphasisedCreationParameters(SpriteText spriteText, bool bold, bool italic)
            => spriteText.Font = spriteText.Font.With(weight: bold ? "Bold" : null, italics: italic);

        SpriteText IMarkdownTextComponent.CreateSpriteText() => CreateSpriteText();
    }
}
