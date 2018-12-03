// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax.Inlines;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Sprites;
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
                                case EmphasisInline _:
                                    var parent = literal.Parent;

                                    var emphases = new List<string>();

                                    while (parent != null && parent is EmphasisInline e)
                                    {
                                        emphases.Add(e.IsDouble ? new string(e.DelimiterChar, 2) : e.DelimiterChar.ToString());
                                        parent = parent.Parent;
                                    }

                                    AddEmphasis(text, emphases);

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
                        if (lineBreak.IsHard)
                            NewParagraph();
                        else
                            NewLine();
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

        protected virtual void AddEmphasis(string text, List<string> emphases)
        {
            var textDrawable = new SpriteText { Text = text };

            bool hasItalic = false;
            bool hasBold = false;

            foreach (var e in emphases)
            {
                switch (e)
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

            string font = "OpenSans-";
            if (hasBold)
                font += "Bold";
            if (hasItalic)
                font += "Italic";

            font = font.Trim('-');

            textDrawable.Font = font;

            AddDrawable(textDrawable);
        }

        protected virtual void AddLinkText(string text, LinkInline linkInline)
            => AddDrawable(new MarkdownLinkText(text, linkInline));

        protected virtual void AddCodeInLine(CodeInline codeInline)
            => AddText(codeInline.Content, t => { t.Colour = Color4.Orange; });

        protected virtual void AddImage(LinkInline linkInline)
            => AddDrawable(new MarkdownImage(linkInline.Url));

        protected virtual void AddNotImplementedInlineText(Inline inline)
            => AddText(inline.GetType() + " not implemented.", t => t.Colour = Color4.Red);
    }
}
