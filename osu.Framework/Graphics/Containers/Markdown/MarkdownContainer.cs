// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Contains all the markdown component <see cref="IMarkdownObject" /> in <see cref="MarkdownDocument" />
    /// </summary>
    public class MarkdownContainer : CompositeDrawable
    {
        private const int root_layer_index = 0;

        private string text = string.Empty;

        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;
                text = value;

                contentCache.Invalidate();
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

        private Cached contentCache = new Cached();

        private readonly FillFlowContainer markdownContainer;

        public MarkdownContainer()
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

        [BackgroundDependencyLoader]
        private void load()
        {
            validateContent();
        }

        private void validateContent()
        {
            if (!contentCache.IsValid)
            {
                var markdownText = Text;
                var pipeline = CreateBuilder();
                var document = Markdig.Markdown.Parse(markdownText, pipeline);

                markdownContainer.Clear();
                foreach (var component in document)
                    AddMarkdownComponent(component, markdownContainer, root_layer_index);

                contentCache.Validate();
            }
        }

        protected override void Update()
        {
            base.Update();

            validateContent();
        }

        protected virtual void AddMarkdownComponent(IMarkdownObject markdownObject, FillFlowContainer container, int layerIndex)
        {
            switch (markdownObject)
            {
                case HeadingBlock headingBlock:
                    container.Add(CreateMarkdownHeading(headingBlock));
                    if (headingBlock.Level < 3)
                        container.Add(CreateMarkdownSeparator());
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
            return new MarkdownTable(table)
            {
                RightSpacing = 100
            };
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

        protected virtual MarkdownSeperator CreateMarkdownSeparator()
        {
            return new MarkdownSeperator();
        }

        protected virtual Drawable CreateNotImplementedMarkdown(IMarkdownObject markdownObject)
            => new NotImplementedMarkdown(markdownObject);

        protected virtual MarkdownPipeline CreateBuilder()
            => new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                                            .UseEmojiAndSmiley()
                                            .UseAdvancedExtensions().Build();
    }
}
