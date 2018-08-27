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
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Contains all the markdown component <see cref="IMarkdownObject" /> in <see cref="MarkdownDocument" />
    /// </summary>
    public class MarkdownContainer : CompositeDrawable
    {
        protected virtual MarkdownPipeline CreateBuilder()
            => new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseEmojiAndSmiley()
            .UseAdvancedExtensions().Build();

        public string Text
        {
            set
            {
                var markdownText = value;
                var pipeline = CreateBuilder();
                var document = Markdig.Markdown.Parse(markdownText, pipeline);

                markdownContainer.Clear();
                foreach (var component in document)
                    AddMarkdownComponent(component, markdownContainer, root_layer_index);
            }
        }

        public virtual float Spacing
        {
            get => markdownContainer.Spacing.Y;
            set => markdownContainer.Spacing = new Vector2(value);
        }

        public MarginPadding MarkdownMargin
        {
            get => markdownContainer.Margin;
            set => markdownContainer.Margin = value;
        }

        public MarginPadding MarkdownPadding
        {
            get => markdownContainer.Padding;
            set => markdownContainer.Padding = value;
        }

        private const int root_layer_index = 0;
        private FillFlowContainer markdownContainer;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new ScrollContainer
                {
                    ScrollbarOverlapsContent = false,
                    RelativeSizeAxes = Axes.Both,
                    Child = markdownContainer = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Direction = FillDirection.Vertical,
                    }
                }
            };

            Spacing = 25;
            MarkdownPadding = new MarginPadding { Left = 10, Right = 30 };
            MarkdownMargin = new MarginPadding { Left = 10, Right = 30 };
        }

        protected virtual void AddMarkdownComponent(IMarkdownObject markdownObject, FillFlowContainer container, int layerIndex)
        {
            switch (markdownObject)
            {
                case HeadingBlock headingBlock:
                    container.Add(CreateMarkdownHeading(headingBlock));
                    if (headingBlock.Level < 3)
                        container.Add(CreateMarkdownSeperator());
                    break;
                case ParagraphBlock paragraphBlock:
                    container.Add(CreateMarkdownTextFlowContainer(paragraphBlock, layerIndex));
                    break;
                case QuoteBlock quoteBlock:
                    container.Add(CreateMarkdownQuoteBlock(quoteBlock));
                    break;
                case FencedCodeBlock fencedCodeBlock:
                    container.Add(CreateMarkdownFencedCodeBlock(fencedCodeBlock));
                    break;
                case Table table:
                    container.Add(CreateMarkdownTable(table));
                    break;
                case ListBlock listBlock:
                    var childContainer = CreateChildFillFlowContainer();
                    container.Add(childContainer);
                    foreach (var single in listBlock)
                        AddMarkdownComponent(single, childContainer, layerIndex + 1);
                    break;
                case ListItemBlock listItemBlock:
                    foreach (var single in listItemBlock)
                        AddMarkdownComponent(single, container, layerIndex);
                    break;
                case HtmlBlock _:
                    //Cannot read Html Syntex in Markdown.
                    break;
                case LinkReferenceDefinitionGroup _:
                    //Link Definition Does not need display.
                    break;
                default:
                    container.Add(CreateNotImplementedMarkdown(markdownObject));
                    break;
            }
        }

        protected virtual MarkdownHeading CreateMarkdownHeading(HeadingBlock headingBlock)
        {
            return new MarkdownHeading(headingBlock);
        }

        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer(ParagraphBlock paragraphBlock, int layerIndex)
        {
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

            drawableParagraphBlock.AddInlineText(paragraphBlock.Inline);
            return drawableParagraphBlock;
        }

        protected virtual MarkdownQuoteBlock CreateMarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            return new MarkdownQuoteBlock(quoteBlock);
        }

        protected virtual MarkdownFencedCodeBlock CreateMarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            return new MarkdownFencedCodeBlock(fencedCodeBlock);
        }

        protected virtual MarkdownTable CreateMarkdownTable(Table table)
        {
            return new MarkdownTable(table);
        }

        protected virtual FillFlowContainer CreateChildFillFlowContainer()
        {
            return new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10, 10),
                Padding = new MarginPadding { Left = 25, Right = 5 },
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
            };
        }

        protected virtual MarkdownSeperator CreateMarkdownSeperator()
        {
            return new MarkdownSeperator();
        }

        protected virtual Drawable CreateNotImplementedMarkdown(IMarkdownObject markdownObject)
        {
            return new NotImplementedMarkdown(markdownObject);
        }
    }

    /// <summary>
    /// Visualises a message when a <see cref="IMarkdownObject"/> doesn't have a visual implementation.
    /// </summary>
    public class NotImplementedMarkdown : CompositeDrawable
    {
        public NotImplementedMarkdown(IMarkdownObject markdownObject)
        {
            AutoSizeAxes = Axes.Y;
            InternalChildren = new SpriteText
            {
                Colour = new Color4(255, 0, 0, 255),
                TextSize = 21,
                Text = markdownObject?.GetType() + " Not implemented."
            };
        }
    }

    /// <summary>
    /// Visualises a markdown table, containing <see cref="MarkdownTableCell"/>s.
    /// </summary>
    public class MarkdownTable : CompositeDrawable
    {
        private readonly MarkdownTableContainer tableContainer;
        private readonly List<List<MarkdownTableCell>> listContainerArray = new List<List<MarkdownTableCell>>();

        protected virtual MarkdownTableCell CreateMarkdownTableCell(TableCell cell, TableColumnDefinition definition, int rowNumber) =>
            new MarkdownTableCell(cell, definition, rowNumber);

        public MarkdownTable(Table table)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Padding = new MarginPadding { Right = 100 };
            Margin = new MarginPadding { Right = 100 };

            foreach (var block in table)
            {
                var tableRow = (TableRow)block;
                List<MarkdownTableCell> rows = new List<MarkdownTableCell>();

                if (tableRow != null)
                    for (int columnIndex = 0; columnIndex < tableRow.Count; columnIndex++)
                    {
                        var columnDimensions = table.ColumnDefinitions[columnIndex];
                        var tableCell = (TableCell)tableRow[columnIndex];
                        if (tableCell != null)
                            rows.Add(CreateMarkdownTableCell(tableCell, columnDimensions, listContainerArray.Count));
                    }

                listContainerArray.Add(rows);
            }

            InternalChild = tableContainer = new MarkdownTableContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Content = listContainerArray.Select(x => x.Select(y => (Drawable)y).ToArray()).ToArray(),
            };
        }

        private Vector2 lastDrawSize;
        protected override void Update()
        {
            if (lastDrawSize != DrawSize)
            {
                lastDrawSize = DrawSize;
                updateColumnDefinitions();
                updateRowDefinitions();
            }
            base.Update();
        }

        private void updateColumnDefinitions()
        {
            var totalColumn = listContainerArray.Max(x => x.Count);
            var totalRows = listContainerArray.Count;

            var listcolumnMaxWidth = new float[totalColumn];

            for (int row = 0; row < totalRows; row++)
            {
                for (int column = 0; column < totalColumn; column++)
                {
                    var colimnTextTotalWidth = listContainerArray[row][column].TextFlowContainer.TotalTextWidth();

                    //get max width
                    listcolumnMaxWidth[column] = Math.Max(listcolumnMaxWidth[column], colimnTextTotalWidth);
                }
            }

            listcolumnMaxWidth = listcolumnMaxWidth.Select(x => x + 20).ToArray();

            var columnDimensions = new Dimension[totalColumn];

            //if max width < DrawWidth, means set absolute value to each column
            if (listcolumnMaxWidth.Sum() < DrawWidth - Margin.Right)
            {
                //not relative , define value instead
                tableContainer.RelativeSizeAxes = Axes.None;
                for (int column = 0; column < totalColumn; column++)
                {
                    columnDimensions[column] = new Dimension(GridSizeMode.Absolute, listcolumnMaxWidth[column]);
                }
            }
            else
            {
                //set to relative
                tableContainer.RelativeSizeAxes = Axes.X;
                var totalWidth = listcolumnMaxWidth.Sum();
                for (int column = 0; column < totalColumn; column++)
                {
                    columnDimensions[column] = new Dimension(GridSizeMode.Relative, listcolumnMaxWidth[column] / totalWidth);
                }
            }
            tableContainer.ColumnDimensions = columnDimensions;
        }

        private void updateRowDefinitions()
        {
            tableContainer.RowDimensions = listContainerArray.Select(x => new Dimension(GridSizeMode.Absolute, x.Max(y => y.TextFlowContainer.DrawHeight + 10))).ToArray();
        }

        private class MarkdownTableContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                get => base.AutoSizeAxes;
                set => base.AutoSizeAxes = value;
            }
        }

        public class MarkdownTableCell : CompositeDrawable
        {
            public MarkdownTextFlowContainer TextFlowContainer => textFlowContainer;
            private readonly MarkdownTextFlowContainer textFlowContainer;

            protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer() =>
                new MarkdownTextFlowContainer
                {
                    Padding = new MarginPadding { Left = 5, Right = 5, Top = 5, Bottom = 0 }
                };

            public MarkdownTableCell(TableCell cell, TableColumnDefinition definition, int rowNumber)
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

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColor,
                        Alpha = backgroundAlpha
                    },
                    textFlowContainer = CreateMarkdownTextFlowContainer()
                };

                foreach (var block in cell)
                {
                    var single = (ParagraphBlock)block;
                    textFlowContainer.ParagraphBlock = single;
                }

                switch (definition.Alignment)
                {
                    case TableColumnAlign.Center:
                        textFlowContainer.TextAnchor = Anchor.TopCentre;
                        break;

                    case TableColumnAlign.Right:
                        textFlowContainer.TextAnchor = Anchor.TopRight;
                        break;

                    default:
                        textFlowContainer.TextAnchor = Anchor.TopLeft;
                        break;
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
    public class MarkdownFencedCodeBlock : CompositeDrawable
    {
        public MarkdownFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            TextFlowContainer textFlowContainer;
            InternalChildren = new Drawable[]
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
    public class MarkdownHeading : CompositeDrawable
    {
        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer() =>
            new MarkdownTextFlowContainer();

        public MarkdownHeading(HeadingBlock headingBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            InternalChildren = new Drawable[]
            {
                textFlowContainer = CreateMarkdownTextFlowContainer()
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
            textFlowContainer.AddInlineText(headingBlock.Inline);
        }
    }

    /// <summary>
    /// MarkdownQuoteBlock :
    /// > NOTE: This document does not describe the `liquid` language.
    /// </summary>
    public class MarkdownQuoteBlock : CompositeDrawable
    {
        protected virtual MarkdownTextFlowContainer CreateMarkdownTextFlowContainer() =>
            new MarkdownTextFlowContainer
            {
                Margin = new MarginPadding { Left = 20 }
            };

        public MarkdownQuoteBlock(QuoteBlock quoteBlock)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            MarkdownTextFlowContainer textFlowContainer;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Gray,
                    Width = 5,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Y
                },
                textFlowContainer = CreateMarkdownTextFlowContainer()
            };

            if (quoteBlock.LastChild is ParagraphBlock paragraphBlock)
                textFlowContainer.ParagraphBlock = paragraphBlock;
        }
    }

    /// <summary>
    /// MarkdownSeperator :
    /// (spacing)
    /// </summary>
    public class MarkdownSeperator : CompositeDrawable
    {
        public MarkdownSeperator()
        {
            Height = 1;
            RelativeSizeAxes = Axes.X;
            InternalChild = new Box();
            {
                RelativeSizeAxes = Axes.X;
                Colour = Color4.Gray;
            }
        }
    }

    /// <summary>
    /// Load image from url
    /// </summary>
    public class MarkdownImage : CompositeDrawable
    {
        private readonly Box background;
        public MarkdownImage(string url)
        {
            InternalChildren = new Drawable[]
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
                            if(d is ImageContainer imageContainer)
                                EffectLoadImageComplete(imageContainer);
                        },
                    })
            };
        }

        protected virtual void EffectLoadImageComplete(ImageContainer imageContainer)
        {
            var rowImageSize = imageContainer.Image?.Texture?.Size ?? new Vector2();
            //Resize to image's row size
            this.ResizeWidthTo(rowImageSize.X, 700, Easing.OutQuint);
            this.ResizeHeightTo(rowImageSize.Y, 700, Easing.OutQuint);

            //Hide background image
            background.FadeTo(0, 300, Easing.OutQuint);
            imageContainer.FadeInFromZero(300, Easing.OutQuint);
        }

        protected class ImageContainer : CompositeDrawable
        {
            private readonly string imageUrl;
            private readonly Sprite image;

            public Sprite Image => image;

            public ImageContainer(string url)
            {
                imageUrl = url;
                InternalChildren = new Drawable[]
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

        public MarkdownTextFlowContainer AddInlineText(ContainerInline lnline)
        {
            foreach (var single in lnline)
            {
                if (single is LiteralInline literalInline)
                {
                    var text = literalInline.Content.ToString();
                    if (lnline.GetNext(literalInline) is HtmlInline
                        && lnline.GetPrevious(literalInline) is HtmlInline)
                        AddText(text, t => t.Colour = Color4.MediumPurple);
                    else if (lnline.GetNext(literalInline) is HtmlEntityInline)
                        AddText(text, t => t.Colour = Color4.GreenYellow);
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
                            AddLinkText(text, literalInline);
                    }
                    else
                        AddText(text);
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
                    AddText(single.GetType() + " Not implemented.", t => t.Colour = Color4.Red);
                }

                //generate child
                if (single is ContainerInline containerInline) AddInlineText(containerInline);
            }

            return this;
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

        protected virtual void AddLinkText(string text, LiteralInline literalInline)
        {
            //TODO Add Link Text
            //var linkText = (literalInline.Parent as LinkInline)?.Url;
            AddText(text, t => t.Colour = Color4.DodgerBlue);
        }

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
            AddImage(new MarkdownImage(imageUrl)
            {
                Width = 40,
                Height = 40,
            });
        }

        protected IEnumerable<SpriteText> AddDrawable(Drawable drawable)
        {
            var imageIndex = AddPlaceholder(drawable);
            return base.AddText("[" + imageIndex + "]");
        }

        public bool IsChangeLine()
        {
            if (FlowingChildren.Any())
            {
                var fortRowX = FlowingChildren.FirstOrDefault()?.BoundingBox.Size.X;
                return FlowingChildren.Any(x => x.BoundingBox.X != fortRowX);
            }
            return true;
        }

        public float TotalTextWidth()
        {
            return FlowingChildren.Sum(x => x.BoundingBox.Size.X);
        }
    }
}
