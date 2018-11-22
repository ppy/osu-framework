// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a markdown text document.
    /// </summary>
    public class MarkdownContainer : CompositeDrawable
    {
        private const int root_level = 0;

        private string text = string.Empty;

        /// <summary>
        /// The text to visualise.
        /// </summary>
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

        /// <summary>
        /// The vertical spacing between lines.
        /// </summary>
        public virtual float LineSpacing
        {
            get => document.Spacing.Y;
            set => document.Spacing = new Vector2(0, value);
        }

        /// <summary>
        /// The margins of the contained document.
        /// </summary>
        public MarginPadding DocumentMargin
        {
            get => document.Margin;
            set => document.Margin = value;
        }

        /// <summary>
        /// The padding of the contained document.
        /// </summary>
        public MarginPadding DocumentPadding
        {
            get => document.Padding;
            set => document.Padding = value;
        }

        private Cached contentCache = new Cached();

        private readonly FillFlowContainer document;

        public MarkdownContainer()
        {
            InternalChildren = new Drawable[]
            {
                new ScrollContainer
                {
                    ScrollbarOverlapsContent = false,
                    RelativeSizeAxes = Axes.Both,
                    Child = document = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Direction = FillDirection.Vertical,
                    }
                }
            };

            LineSpacing = 25;
            DocumentPadding = new MarginPadding { Left = 10, Right = 30 };
            DocumentMargin = new MarginPadding { Left = 10, Right = 30 };
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
                var parsed = Markdig.Markdown.Parse(markdownText, pipeline);

                document.Clear();
                foreach (var component in parsed)
                    AddMarkdownComponent(component, document, root_level);

                contentCache.Validate();
            }
        }

        protected override void Update()
        {
            base.Update();

            validateContent();
        }

        /// <summary>
        /// Adds a component that visualises a <see cref="IMarkdownObject"/> to the document.
        /// </summary>
        /// <param name="markdownObject">The <see cref="IMarkdownObject"/> to visualise.</param>
        /// <param name="container">The container to add the visualisation to.</param>
        /// <param name="level">The level in the document of <paramref name="markdownObject"/>.
        /// 0 for the root level, 1 for first-level items in a list, 2 for second-level items in a list, etc.</param>
        protected virtual void AddMarkdownComponent(IMarkdownObject markdownObject, FillFlowContainer container, int level)
        {
            switch (markdownObject)
            {
                case HeadingBlock headingBlock:
                    container.Add(CreateHeading(headingBlock));
                    if (headingBlock.Level < 3)
                        container.Add(CreateSeparator());
                    break;
                case ParagraphBlock paragraphBlock:
                    container.Add(CreateParagraph(paragraphBlock, level));
                    break;
                case QuoteBlock quoteBlock:
                    container.Add(CreateQuoteBlock(quoteBlock));
                    break;
                case FencedCodeBlock fencedCodeBlock:
                    container.Add(CreateFencedCodeBlock(fencedCodeBlock));
                    break;
                case Table table:
                    container.Add(CreateTable(table));
                    break;
                case ListBlock listBlock:
                    var childContainer = CreateList();
                    container.Add(childContainer);
                    foreach (var single in listBlock)
                        AddMarkdownComponent(single, childContainer, level + 1);
                    break;
                case ListItemBlock listItemBlock:
                    foreach (var single in listItemBlock)
                        AddMarkdownComponent(single, container, level);
                    break;
                case HtmlBlock _:
                    // HTML is not supported
                    break;
                case LinkReferenceDefinitionGroup _:
                    // Link reference doesn't need to be displayed.
                    break;
                default:
                    container.Add(CreateNotImplemented(markdownObject));
                    break;
            }
        }

        /// <summary>
        /// Visualises a <see cref="HeadingBlock"/>.
        /// </summary>
        /// <param name="headingBlock">The <see cref="HeadingBlock"/> to visualise.</param>
        /// <returns>The visualisation.</returns>
        protected virtual MarkdownHeading CreateHeading(HeadingBlock headingBlock) => new MarkdownHeading(headingBlock);

        protected virtual MarkdownParagraph CreateParagraph(ParagraphBlock paragraphBlock, int level) => new MarkdownParagraph(paragraphBlock, level);

        protected virtual MarkdownQuoteBlock CreateQuoteBlock(QuoteBlock quoteBlock) => new MarkdownQuoteBlock(quoteBlock);

        protected virtual MarkdownFencedCodeBlock CreateFencedCodeBlock(FencedCodeBlock fencedCodeBlock) => new MarkdownFencedCodeBlock(fencedCodeBlock);

        protected virtual MarkdownTable CreateTable(Table table) => new MarkdownTable(table)
        {
            RightSpacing = 100
        };

        protected virtual FillFlowContainer CreateList() => new MarkdownList();

        protected virtual MarkdownSeparator CreateSeparator() => new MarkdownSeparator();

        protected virtual Drawable CreateNotImplemented(IMarkdownObject markdownObject) => new NotImplementedMarkdown(markdownObject);

        protected virtual MarkdownPipeline CreateBuilder()
            => new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                                            .UseEmojiAndSmiley()
                                            .UseAdvancedExtensions().Build();
    }
}
