// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax;
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
        private ParagraphBlock paragraphBlock;

        public ParagraphBlock ParagraphBlock
        {
            get => paragraphBlock;
            set
            {
                paragraphBlock = value;
                Clear();
                AddInlineText(paragraphBlock.Inline);
            }
        }

        public float TotalTextWidth => Padding.TotalHorizontal + FlowingChildren.Sum(x => x.BoundingBox.Size.X);

        public MarkdownTextFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected IEnumerable<SpriteText> AddDrawable(Drawable drawable)
        {
            var imageIndex = AddPlaceholder(drawable);
            return base.AddText("[" + imageIndex + "]");
        }

        public new IEnumerable<SpriteText> AddText(string text, Action<SpriteText> creationParameters = null)
        {
            text = text.Replace("[", "[[").Replace("]", "]]");
            return base.AddText(text, creationParameters);
        }

        public new IEnumerable<SpriteText> AddParagraph(string text, Action<SpriteText> creationParameters = null)
        {
            text = text.Replace("[", "[[").Replace("]", "]]");
            return base.AddParagraph(text, creationParameters);
        }

        public MarkdownTextFlowContainer AddInlineText(ContainerInline lnline)
        {
            foreach (var single in lnline)
            {
                if (single is LiteralInline literalInline)
                {
                    var text = literalInline.Content.ToString();

                    if (lnline.GetNext(literalInline) is HtmlInline
                        && lnline.GetPrevious(literalInline) is HtmlInline)
                    {
                        AddHtmlInLineText(text, literalInline);
                    }
                    else if (lnline.GetNext(literalInline) is HtmlEntityInline)
                    {
                        AddHtmlEntityInlineText(text, literalInline);
                    }
                    else if (literalInline.Parent is EmphasisInline emphasisInline)
                    {
                        if (emphasisInline.IsDouble)
                        {
                            switch (emphasisInline.DelimiterChar)
                            {
                                case '*':
                                    AddBoldText(text, literalInline);
                                    break;
                                default:
                                    AddDefalutLiteralInlineText(text, literalInline);
                                    break;
                            }
                        }
                        else
                        {
                            AddDefalutLiteralInlineText(text, literalInline);
                        }
                    }
                    else if (literalInline.Parent is LinkInline linkInline)
                    {
                        if (!linkInline.IsImage)
                            AddLinkText(linkInline, text);
                    }
                    else
                    {
                        AddDefalutLiteralInlineText(text, literalInline);
                    }
                }
                else if (single is CodeInline codeInline)
                {
                    AddCodeInLineText(codeInline);
                }
                else if (single is LinkInline linkInline)
                {
                    if (linkInline.IsImage)
                    {
                        AddImage(linkInline);
                    }
                }
                else if (single is HtmlInline || single is HtmlEntityInline || single is EmphasisInline)
                {
                    //DO nothing
                }
                else if (single is LineBreakInline)
                {
                    //IDK what is this but just ignore
                }
                else
                {
                    AddNotImpiementedInlineText(single);
                }

                //generate child
                if (single is ContainerInline containerInline)
                    AddInlineText(containerInline);
            }

            return this;
        }

        protected virtual void AddHtmlInLineText(string text, LiteralInline literalInline)
        {
            AddText(text, t => t.Colour = Color4.MediumPurple);
        }

        protected virtual void AddHtmlEntityInlineText(string text, LiteralInline literalInline)
        {
            AddText(text, t => t.Colour = Color4.GreenYellow);
        }

        protected virtual void AddBoldText(string text, LiteralInline literalInline)
        {
            //TODO : make real "Bold text"
            AddDrawable(new SpriteText
            {
                Text = text,
                Colour = Color4.LightGray
            }.WithEffect(new GlowEffect
            {
                BlurSigma = new Vector2(1f),
                Strength = 2f,
                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 1.2f, 1.2f, 1f), new Color4(1.2f, 1.2f, 1.2f, 1f)),
            }));
        }

        protected virtual void AddLinkText(LinkInline linkInline, string text)
            => AddDrawable(new MarkdownLinkText(linkInline, text));

        protected virtual void AddDefalutLiteralInlineText(string text, LiteralInline literalInline)
        {
            AddText(text);
        }

        protected virtual void AddCodeInLineText(CodeInline codeInline)
        {
            AddText(codeInline.Content, t =>
            {
                t.Colour = Color4.Orange;
            });
        }

        protected virtual void AddImage(LinkInline linkInline)
        {
            var imageUrl = linkInline.Url;
            //insert a image
            AddDrawable(new MarkdownImage(imageUrl));
        }

        protected virtual void AddNotImpiementedInlineText(Inline inline)
        {
            AddText(inline.GetType() + " Not implemented.", t => t.Colour = Color4.Red);
        }
    }
}
