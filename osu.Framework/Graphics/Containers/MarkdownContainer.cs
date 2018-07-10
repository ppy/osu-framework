// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Contains all the markdown component <see cref="IMarkdownObject" /> in <see cref="MarkdownDocument" />
    /// </summary>
    public class MarkdownContainer : ScrollContainer
    {
        public MarkdownDocument MarkdownDocument
        {
            get => document;
            set
            {
                document = value;
                //clear all exist markdown object and re-create them
                markdownContainer.Clear();
                foreach (var component in document)
                    AddMarkdownComponent(component, markdownContainer, root_layer_index);
            }
        }

        public string MarkdownText
        {
            set
            {
                var markdownText = value;
                var pipeline = new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                    .UseEmojiAndSmiley()
                    .UseAdvancedExtensions().Build();
                MarkdownDocument = Markdown.Parse(markdownText, pipeline);
            }
        }

        private const int root_layer_index = 0;
        private const int seperator_px = 25;
        private MarkdownDocument document;
        private readonly FillFlowContainer markdownContainer;

        public MarkdownContainer()
        {
            ScrollbarOverlapsContent = false;
            Child = markdownContainer = new FillFlowContainer
            {
                Padding = new MarginPadding { Left = 10, Right = 30 },
                Margin = new MarginPadding { Left = 10, Right = 30 },
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(seperator_px)
            };
        }

        protected void AddMarkdownComponent(IMarkdownObject markdownObject, FillFlowContainer container, int layerIndex)
        {
            switch (markdownObject)
            {
                case HeadingBlock headingBlock:
                    container.Add(new MarkdownHeading(headingBlock));
                    break;
                case ParagraphBlock paragraphBlock:
                    var drawableParagraphBlock = new MarkdownTextFlowContainer();
                    switch (layerIndex)
                    {
                        case 1:
                            drawableParagraphBlock.AddText("@ ", t => t.Colour = Color4.DarkGray);
                            break;
                        case 2:
                            drawableParagraphBlock.AddText("# ", t => t.Colour = Color4.DarkGray);
                            break;
                        case 3:
                        case 4:
                            drawableParagraphBlock.AddText("+ ", t => t.Colour = Color4.DarkGray);
                            break;
                    }

                    drawableParagraphBlock = ParagraphBlockHelper.GeneratePartial(drawableParagraphBlock, paragraphBlock.Inline);
                    container.Add(drawableParagraphBlock);
                    break;
                case QuoteBlock quoteBlock:
                    container.Add(new MarkdownQuoteBlock(quoteBlock));
                    break;
                case FencedCodeBlock fencedCodeBlock:
                    container.Add(new MarkdownFencedCodeBlock(fencedCodeBlock));
                    break;
                case Table table:
                    container.Add(new MarkdownTable(table));
                    break;
                case ListBlock listBlock:
                    var childContainer = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(10, 10),
                        Padding = new MarginPadding { Left = 25, Right = 5 },
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                    };
                    container.Add(childContainer);
                    foreach (var single in listBlock)
                    {
                        AddMarkdownComponent(single, childContainer, layerIndex + 1);
                    }
                    break;
                case ListItemBlock listItemBlock:
                    foreach (var single in listItemBlock)
                    {
                        AddMarkdownComponent(single, container, layerIndex);
                    }
                    break;
                default:
                    container.Add(new NotExistingMarkdown(markdownObject));
                    break;
            }


            //show seperator line
            if (markdownObject is LeafBlock leafBlock && !(markdownObject is ParagraphBlock))
            {
                if (leafBlock.Inline != null)
                {
                    container.Add(new MarkdownSeperator());
                }
            }
        }
    }

    /// <summary>
    /// NotExistMarkdown :
    /// shows the <see cref="IMarkdownObject" /> does not implement in drawable object
    /// </summary>
    internal class NotExistingMarkdown : SpriteText
    {
        public NotExistingMarkdown(IMarkdownObject markdownObject)
        {
            Colour = new Color4(255, 0, 0, 255);
            TextSize = 21;
            Text = markdownObject?.GetType() + " Not implemented.";
        }
    }

    /// <summary>
    /// MarkdownTable : 
    /// |Operator            | Description
    /// |--------------------|------------
    /// | `<left/> + <right/>` | add left to right number 
    /// | `<left/> - <right/>` | substract right number from left
    /// | `<left/> * <right/>` | multiply left by right number
    /// | `<left/> / <right/>` | divide left by right number
    /// | `<left/> // <right/>`| divide left by right number and round to an integer
    /// | `<left/> % <right/>` | calculates the modulus of left by right
    /// </summary>
    internal class MarkdownTable : Container
    {
        private readonly MarkdownTableContainer tableContainer;
        private readonly List<List<MarkdownTableCell>> listContainerArray = new List<List<MarkdownTableCell>>();
        public MarkdownTable(Table table)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Padding = new MarginPadding { Right = 100 };

            foreach (var block in table)
            {
                var tableRow = (TableRow)block;
                List<MarkdownTableCell> rows = new List<MarkdownTableCell>();

                if (tableRow != null)
                    foreach (var block1 in tableRow)
                    {
                        var tableCell = (TableCell)block1;
                        if (tableCell != null)
                            rows.Add(new MarkdownTableCell(tableCell, listContainerArray.Count));
                    }

                listContainerArray.Add(rows);
            }

            Children = new Drawable[]
            {
                tableContainer = new MarkdownTableContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Content = listContainerArray.Select(x=>x.Select(y=>(Drawable)y).ToArray()).ToArray(),
                }
            };

            //define max row is 50
            tableContainer.RowDimensions = Enumerable.Repeat(new Dimension(GridSizeMode.AutoSize), 50).ToArray();

            int row = listContainerArray.FirstOrDefault()?.Count ?? 0;

            if (row == 2)
            {
                tableContainer.ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.3f) };
            }
        }

        protected override void Update()
        {
            tableContainer.RowDimensions = listContainerArray.Select(X => new Dimension(GridSizeMode.Absolute, X.Max(Y => Y.TextFlowContainer.DrawHeight + 10))).ToArray();
            base.Update();
        }


        private class MarkdownTableContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                get => base.AutoSizeAxes;
                set => base.AutoSizeAxes = value;
            }
        }

        private class MarkdownTableCell : Container
        {
            public MarkdownTextFlowContainer TextFlowContainer => textFlowContainer;
            private readonly MarkdownTextFlowContainer textFlowContainer;

            public MarkdownTableCell(TableCell cell, int rowNumber)
            {
                RelativeSizeAxes = Axes.Both;
                BorderThickness = 1.8f;
                BorderColour = Color4.White;
                Masking = true;

                var backgroundColor = rowNumber % 2 != 0 ? Color4.White : Color4.LightGray;
                var backgroundAlpha = 0.3f;
                if (rowNumber == 0)
                {
                    backgroundColor = Color4.White;
                    backgroundAlpha = 0.4f;
                }

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColor,
                        Alpha = backgroundAlpha
                    },
                    textFlowContainer = new MarkdownTextFlowContainer
                    {
                        Margin = new MarginPadding{Left = 5,Right = 5,Top = 5,Bottom = 5}
                    }
                };

                foreach (var block in cell)
                {
                    var single = (ParagraphBlock)block;
                    ParagraphBlockHelper.GeneratePartial(textFlowContainer, single.Inline);
                }
            }
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
        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            TextFlowContainer textFlowContainer;
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray,
                    Alpha = 0.5f
                },
                textFlowContainer = new TextFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Margin = new MarginPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 }
                }
            };

            var lines = fencedCodeBlock.Lines.Lines.Take(fencedCodeBlock.Lines.Count);
            foreach (var sligneLine in lines)
            {
                var lineString = sligneLine.ToString();
                textFlowContainer.AddParagraph(lineString);
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
    internal class MarkdownHeading : Container
    {
        public MarkdownHeading(HeadingBlock headingBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            Children = new Drawable[]
            {
                textFlowContainer = new MarkdownTextFlowContainer()
            };

            var level = headingBlock.Level;
            Vector2 scale = new Vector2(1);

            switch (level)
            {
                case 1:
                    scale = new Vector2(2.7f);
                    break;
                case 2:
                    scale = new Vector2(2);
                    break;
                case 3:
                    scale = new Vector2(1.5f);
                    break;
                case 4:
                    scale = new Vector2(1.3f);
                    break;
            }

            textFlowContainer.Scale = scale;
            ParagraphBlockHelper.GeneratePartial(textFlowContainer, headingBlock.Inline);
        }
    }

    /// <summary>
    /// MarkdownQuoteBlock :
    /// > NOTE: This document does not describe the `liquid` language.
    /// </summary>
    internal class MarkdownQuoteBlock : Container
    {
        public MarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            Children = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Gray,
                    Width = 5,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Y
                },
                textFlowContainer = new MarkdownTextFlowContainer
                {
                    Margin = new MarginPadding { Left = 20 }
                }
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
                ParagraphBlockHelper.GeneratePartial(textFlowContainer, paragraphBlock.Inline);
        }
    }

    /// <summary>
    /// MarkdownSeperator :
    /// (spacing)
    /// </summary>
    internal class MarkdownSeperator : Box
    {
        public MarkdownSeperator()
        {
            RelativeSizeAxes = Axes.X;
            Height = 1;
            Colour = Color4.Gray;
        }
    }

    /// <summary>
    /// Fill <see cref="Inline" /> into <see cref="TextFlowContainer" />
    /// </summary>
    internal static class ParagraphBlockHelper
    {
        public static MarkdownTextFlowContainer GenerateText(ParagraphBlock paragraphBlock)
        {
            var textFlowContainer = new MarkdownTextFlowContainer();
            GeneratePartial(textFlowContainer, paragraphBlock.Inline);
            return textFlowContainer;
        }

        public static MarkdownTextFlowContainer GeneratePartial(MarkdownTextFlowContainer textFlowContainer, ContainerInline lnline)
        {
            foreach (var single in lnline)
            {
                if (single is LiteralInline literalInline)
                {
                    var text = literalInline.Content.ToString();
                    if (lnline.GetNext(literalInline) is HtmlInline
                        && lnline.GetPrevious(literalInline) is HtmlInline)
                        textFlowContainer.AddText(text, t => t.Colour = Color4.MediumPurple);
                    else if (lnline.GetNext(literalInline) is HtmlEntityInline)
                        textFlowContainer.AddText(text, t => t.Colour = Color4.GreenYellow);
                    else if (literalInline.Parent is LinkInline linkInline)
                    {
                        if (!linkInline.IsImage)
                            textFlowContainer.AddText(text, t => t.Colour = Color4.DodgerBlue);
                    }
                    else
                        textFlowContainer.AddText(text);
                }
                else if (single is CodeInline codeInline)
                {
                    textFlowContainer.AddText(codeInline.Content, t => t.Colour = Color4.Orange);
                }
                else if (single is EmphasisInline)
                {
                    //foreach (var child in emphasisInline)
                    //{
                    //    textFlowContainer.AddText(child.ToString());
                    //}
                }
                else if (single is LinkInline linkInline)
                {
                    if (linkInline.IsImage)
                    {
                        var imageUrl = linkInline.Url;
                        //insert a image
                        textFlowContainer.AddImage(new MarkdownImage(imageUrl)
                        {
                            Width = 300,
                            Height = 240,
                        });
                    }
                }
                else if (single is HtmlInline || single is HtmlEntityInline)
                {
                    //DO nothing
                }
                else if (single is LineBreakInline)
                {
                    //IDK what is this but just ignore
                }
                else
                {
                    textFlowContainer.AddText(single.GetType() + " Not implemented.", t => t.Colour = Color4.Red);
                }

                //generate child
                if (single is ContainerInline containerInline) GeneratePartial(textFlowContainer, containerInline);
            }

            return textFlowContainer;
        }
    }

    /// <summary>
    /// Load image from url
    /// </summary>
    internal class MarkdownImage : Container
    {
        public MarkdownImage(string url)
        {
            Box background;
            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.LightGray,
                    Alpha = 0.3f
                },
                new DelayedLoadWrapper(
                    new ImageContainer(url)
                    {
                        RelativeSizeAxes = Axes.Both,
                        OnLoadComplete = d =>
                        {
                            background.FadeTo(0,300,Easing.OutQuint);
                            d.FadeInFromZero(300, Easing.OutQuint);
                        },
                    })
            };
        }

        private class ImageContainer : Container
        {
            private readonly string imageUrl;
            private readonly Sprite image;

            public ImageContainer(string url)
            {
                imageUrl = url;
                Children = new Drawable[]
                {
                    image = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Texture texture = null;
                if (!string.IsNullOrEmpty(imageUrl))
                    texture = textures.Get(imageUrl);

                //TODO : get default texture
                //if (texture == null)
                //    texture = textures.Get(@"Markdown/default-image");

                image.Texture = texture;
            }
        }
    }

    /// <summary>
    /// Markdown text flow container.
    /// </summary>
    internal class MarkdownTextFlowContainer : CustomizableTextContainer
    {
        public MarkdownTextFlowContainer()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        public IEnumerable<SpriteText> AddImage(MarkdownImage image)
        {
            var imageIndex = AddPlaceholder(image);
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
    }
}
