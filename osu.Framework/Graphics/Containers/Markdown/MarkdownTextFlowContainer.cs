// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax.Inlines;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Markdown text flow container.
    /// </summary>
    public class MarkdownTextFlowContainer : CustomizableTextContainer
    {
        public float TotalTextWidth => Padding.TotalHorizontal + FlowingChildren.Sum(x => x.BoundingBox.Size.X);

        public MarkdownTextFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected void AddDrawable(Drawable drawable)
            => base.AddText("[" + AddPlaceholder(drawable) + "]");

        public new void AddText(string text, Action<SpriteText> creationParameters = null)
            => base.AddText(text.Replace("[", "[[").Replace("]", "]]"), creationParameters);

        public new IEnumerable<SpriteText> AddParagraph(string text, Action<SpriteText> creationParameters = null)
            => base.AddParagraph(text.Replace("[", "[[").Replace("]", "]]"), creationParameters);

        public void AddInlineText(ContainerInline container)
        {
            foreach (var single in container)
            {
                switch (single)
                {
                    case LiteralInline literal:
                        var text = literal.Content.ToString();

                        if (container.GetPrevious(literal) is HtmlInline && container.GetNext(literal) is HtmlInline)
                            AddHtmlInLineText(text, literal);
                        else if (container.GetNext(literal) is HtmlEntityInline entityInLine)
                            AddHtmlEntityInlineText(text, entityInLine);
                        else
                        {
                            switch (literal.Parent)
                            {
                                case EmphasisInline emphasisInline when emphasisInline.IsDouble:
                                    AddStrongEmphasis(text, emphasisInline);
                                    break;
                                case EmphasisInline emphasisInline:
                                    AddEmphasis(text, emphasisInline);
                                    break;
                                case LinkInline linkInline:
                                {
                                    if (!linkInline.IsImage)
                                        AddLinkText(text, linkInline);
                                    break;
                                }
                                default:
                                    AddText(text);
                                    break;
                            }
                        }
                        break;
                    case CodeInline codeInline:
                        AddCodeInLine(codeInline);
                        break;
                    case LinkInline linkInline when linkInline.IsImage:
                        AddImage(linkInline);
                        break;
                    case HtmlInline _:
                    case HtmlEntityInline _:
                        // Handled by the next literal
                        break;
                    case LineBreakInline lineBreak:
                        // Todo: Why was this empty
                        break;
                    case ContainerInline innerContainer:
                        AddInlineText(innerContainer);
                        break;
                    default:
                        AddNotImplementedInlineText(single);
                        break;
                }
            }
        }

        protected virtual void AddHtmlInLineText(string text, LiteralInline literalInline)
            => AddText(text, t => t.Colour = Color4.MediumPurple);

        protected virtual void AddHtmlEntityInlineText(string text, HtmlEntityInline entityInLine)
            => AddText(text, t => t.Colour = Color4.GreenYellow);

        protected virtual void AddEmphasis(string text, EmphasisInline emphasisInLine)
        {
            switch (emphasisInLine.DelimiterChar)
            {
                case '*':
                case '_':
                    AddText(text);
                    break;
            }
        }

        protected virtual void AddStrongEmphasis(string text, EmphasisInline emphasisInLine)
        {
            switch (emphasisInLine.DelimiterChar)
            {
                case '*':
                case '_':
                    AddDrawable(CreateBoldText(text));
                    break;
            }
        }

        protected virtual void AddLinkText(string text, LinkInline linkInline)
            => AddDrawable(new MarkdownLinkText(text, linkInline));

        protected virtual void AddCodeInLine(CodeInline codeInline)
            => AddText(codeInline.Content, t => { t.Colour = Color4.Orange; });

        protected virtual void AddImage(LinkInline linkInline)
            => AddDrawable(new MarkdownImage(linkInline.Url));

        protected virtual void AddNotImplementedInlineText(Inline inline)
            => AddText(inline.GetType() + " not implemented.", t => t.Colour = Color4.Red);

        protected virtual Drawable CreateBoldText(string text) => new SpriteText
        {
            Text = text,
            Colour = Color4.LightGray
        }.WithEffect(new GlowEffect // Todo: osu!framework doesn't have a bold font yet
        {
            BlurSigma = new Vector2(1f),
            Strength = 2f,
            Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 1.2f, 1.2f, 1f), new Color4(1.2f, 1.2f, 1.2f, 1f)),
        });
    }
}
