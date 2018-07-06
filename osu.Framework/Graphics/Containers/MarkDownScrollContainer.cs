// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// MarkDownScrollContainer
    /// </summary>
    public class MarkdownScrollContainer : ScrollContainer
    {
        private readonly MarkdownContainer _markdownContainer;

        public MarkdownScrollContainer()
        {
            ScrollbarOverlapsContent = false;
            Child = _markdownContainer = new MarkdownContainer()
            {
                Padding = new MarginPadding(3),
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
            };
        }

        public MarkdownDocument MarkdownDocument
        {
            get => _markdownContainer.MarkdownDocument;
            set => _markdownContainer.MarkdownDocument = value;
        }
    }

    /// <summary>
    /// MarkdownContainer
    /// Contains  all the markdown component <see cref="IMarkdownObject"/> in <see cref="MarkdownDocument"/>
    /// </summary>
    public class MarkdownContainer : FillFlowContainer
    {
        public MarkdownContainer()
        {
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(20, 20);
            Margin = new MarginPadding() { Left = 20, Right = 20 };
        }

        private MarkdownDocument _document;

        public MarkdownDocument MarkdownDocument
        {
            get => _document;
            set
            {
                _document = value;
                //clear all exist markdown object
                Clear();

                //start creating
                int rootLayerIndex = 0;
                foreach (var component in _document)
                {
                    AddMarkdownComponent(component, this, rootLayerIndex);
                }
            }
        }

        public void AddMarkdownComponent(IMarkdownObject markdownObject, FillFlowContainer container, int layerIndex)
        {
            if (markdownObject is HeadingBlock headingBlock)
            {
                container.Add(new MarkdownHeadingBlock(headingBlock));
            }
            else if (markdownObject is LiteralInline literalInline)
            {
                container.Add(new MarkdownSeperator(literalInline));
            }
            else if (markdownObject is ParagraphBlock paragraphBlock)
            {
                var drawableParagraphBlock = ParagraphBlockHelper.GenerateText(paragraphBlock);
                drawableParagraphBlock.RelativeSizeAxes = Axes.X;
                drawableParagraphBlock.AutoSizeAxes = Axes.Y;
                container.Add(drawableParagraphBlock);
            }
            else if (markdownObject is QuoteBlock quoteBlock)
            {
                container.Add(new MarkdownQuoteBlock(quoteBlock));
            }
            else if (markdownObject is ListBlock listBlock)
            {
                container.Add(new MarkdownListBlock(listBlock));
            }
            else if (markdownObject is FencedCodeBlock fencedCodeBlock)
            {
                container.Add(new MarkdownFencedCodeBlock(fencedCodeBlock));
            }
            else
            {
                container.Add(new NotExistMarkdown(markdownObject));
            }

            //show child object
            if (markdownObject is LeafBlock leafBlock && !(markdownObject is ParagraphBlock))
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var single in leafBlock.Inline)
                    {
                        //TODO : if mant to insert markdown object recursive , use this instead.
                        /*
                        var childContainer = new FillFlowContainer()
                        {
                            Direction = FillDirection.Vertical,
                            Spacing = new OpenTK.Vector2(10, 10),
                            Margin = new MarginPadding(){Left = 20,Right = 20},
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                        };
                        container.Add(childContainer);
                        AddMarkdownComponent(single,childContainer,layerIndex + 1);
                        */

                        AddMarkdownComponent(single, container, layerIndex + 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// NotExistMarkdown : 
    /// shows the <see cref="IMarkdownObject"/> does not implement in drawable object
    /// </summary>
    internal class NotExistMarkdown : SpriteText
    {
        public NotExistMarkdown(IMarkdownObject markdownObject)
        {
            Colour = new Color4(255, 0, 0, 255);
            TextSize = 21;
            Text = markdownObject?.GetType() + "Does not be implemented";
        }
    }

    /// <summary>
    /// MarkdownFencedCodeBlock :
    /// ```
    /// foo
    /// ```
    /// </summary>
    internal class MarkdownFencedCodeBlock : Container
    {
        private readonly TextFlowContainer _textFlowContainer;

        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray,
                    Alpha = 0.5f,
                },
                _textFlowContainer = new TextFlowContainer
                {
                    Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 },
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };

            var lines = fencedCodeBlock.Lines.Lines.Take(fencedCodeBlock.Lines.Count);
            foreach (var sligneLine in lines)
            {
                var lineString = sligneLine.ToString();
                _textFlowContainer.AddParagraph(lineString);
            }
        }
    }

    /// <summary>
    /// NotExistMarkdown : 
    /// - [1. Blocks](#1-blocks)
    ///     - [1.1 Code block](#11-code-block)
    ///     - [1.2 Text block](#12-text-block)
    ///     - [1.3 Escape block](#13-escape-block)
    ///     - [1.4 Whitespace control](#14-whitespace-control)
    /// </summary>
    internal class MarkdownListBlock : FillFlowContainer
    {
        public MarkdownListBlock(ListBlock listBlock)
        {
            Direction = FillDirection.Vertical;
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            int rootLayerIndex = 0;
            CreateLayer(listBlock, rootLayerIndex);
        }

        void CreateLayer(ListBlock listBlock, int layerIndex)
        {
            foreach (var singleBlock in listBlock)
            {
                //TODO : singleBlock has two child
                //[0] : 1. Blocks
                //[1] : 1.1 Code block
                //      1.2 Text block
                //      1.3 Escape block
                //      1.4 Whitespace control

                if (singleBlock is ListItemBlock listitemBlock)
                {
                    foreach (var block in listitemBlock)
                    {
                        if (block is ParagraphBlock paragraphBlock)
                        {
                            var drawableParagraphBlock = ParagraphBlockHelper.GenerateText(paragraphBlock);
                            drawableParagraphBlock.Margin = new MarginPadding() { Left = 20 * layerIndex };
                            drawableParagraphBlock.RelativeSizeAxes = Axes.X;
                            drawableParagraphBlock.AutoSizeAxes = Axes.Y;
                            Add(drawableParagraphBlock);
                        }
                        else if (block is ListBlock listBlock2)
                        {
                            CreateLayer(listBlock2, layerIndex + 1);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// MarkdownHeading : 
    /// #Heading1
    /// ##Heading2
    /// ###Heading3
    /// ###3Heading4
    /// </summary>
    internal class MarkdownHeadingBlock : Container
    {
        private readonly TextFlowContainer _textFlowContainer;

        public MarkdownHeadingBlock(HeadingBlock headingBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Children = new Drawable[]
            {
                _textFlowContainer = new TextFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };

            var level = headingBlock.Level;
            string text = headingBlock.Inline.FirstChild.ToString();
            int textSize = 10;

            switch (level)
            {
                case 1:
                    textSize = 50;
                    break;
                case 2:
                    textSize = 38;
                    break;
                case 3:
                    textSize = 28;
                    break;
                case 4:
                    textSize = 21;
                    break;
                case 5:
                    textSize = 10;
                    break;
            }

            _textFlowContainer.AddText(text, t => t.TextSize = textSize);
            _textFlowContainer = ParagraphBlockHelper.GeneratePartial(_textFlowContainer, headingBlock.Inline);
        }
    }

    /// <summary>
    /// MarkdownQuoteBlock : 
    /// > NOTE: This document does not describe the `liquid` language. Check the [`liquid website`](https://shopify.github.io/liquid/) directly.
    /// </summary>
    internal class MarkdownQuoteBlock : Container
    {
        private readonly TextFlowContainer _textFlowContainer;
        private Box _quoteBox;

        public MarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Children = new Drawable[]
            {
                _quoteBox = new Box()
                {
                    Colour = Color4.Gray,
                    Width = 5,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Y,
                },
                _textFlowContainer = new TextFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Margin = new MarginPadding() { Left = 20 }
                }
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
            {
                _textFlowContainer = ParagraphBlockHelper.GeneratePartial(_textFlowContainer, paragraphBlock.Inline);
            }
        }
    }

    /// <summary>
    /// MarkdownSeperator : 
    /// (spacing)
    /// </summary>
    internal class MarkdownSeperator : Box
    {
        public MarkdownSeperator(LiteralInline ParagraphBlock)
        {
            RelativeSizeAxes = Axes.X;
            Height = 1;
            Colour = Color4.Gray;
        }
    }

    /// <summary>
    /// Fill <see cref="Inline"/> into <see cref="TextFlowContainer"/>
    /// </summary>
    internal static class ParagraphBlockHelper
    {
        public static TextFlowContainer GenerateText(ParagraphBlock paragraphBlock)
        {
            TextFlowContainer textFlowContainer = new TextFlowContainer();
            GeneratePartial(textFlowContainer, paragraphBlock.Inline);
            return textFlowContainer;
        }

        public static TextFlowContainer GeneratePartial(TextFlowContainer textFlowContainer, ContainerInline lnline)
        {
            foreach (var single in lnline)
            {
                if (single is LiteralInline literalInline)
                {
                    var text = literalInline.Content.ToString();
                    if (lnline.GetNext(literalInline) is HtmlInline
                        && lnline.GetPrevious(literalInline) is HtmlInline htmlInline)
                    {
                        textFlowContainer.AddText(text, t => t.Colour = Color4.MediumPurple);
                    }
                    else if (lnline.GetNext(literalInline) is HtmlEntityInline htmlEntityInline)
                    {
                        textFlowContainer.AddText(text, t => t.Colour = Color4.LawnGreen);
                    }
                    else if (literalInline.Parent is LinkInline linkInline)
                    {
                        textFlowContainer.AddText(text, t => t.Colour = Color4.DodgerBlue);
                    }
                    //else if(literalInline.Parent is HeadingBlock headingBlock)
                    //{
                    //    
                    //}
                    else
                    {
                        textFlowContainer.AddText(text);
                    }
                }
                else if (single is CodeInline codeInline)
                {
                    textFlowContainer.AddText(codeInline.Content, t => t.Colour = Color4.Orange);
                }
                else if (single is EmphasisInline emphasisInline)
                {
                    //foreach (var child in emphasisInline)
                    //{
                    //    textFlowContainer.AddText(child.ToString());
                    //}
                }
                else if (single is LinkInline || single is HtmlInline || single is HtmlEntityInline)
                {
                    //DO nothing
                }
                else if (single is LineBreakInline)
                {
                    //IDK what is this but just ignore
                }
                else
                {
                    textFlowContainer.AddText(single.GetType().ToString(), t => t.Colour = Color4.Red);
                }

                //generate child
                if (single is ContainerInline containerInline)
                {
                    GeneratePartial(textFlowContainer, containerInline);
                }
            }

            return textFlowContainer;
        }
    }
}
