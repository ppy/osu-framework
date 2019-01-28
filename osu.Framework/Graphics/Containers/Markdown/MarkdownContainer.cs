﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a markdown text document.
    /// </summary>
    [Cached(Type = typeof(IMarkdownTextComponent))]
    [Cached(Type = typeof(IMarkdownTextFlowComponent))]
    public class MarkdownContainer : CompositeDrawable, IMarkdownTextComponent, IMarkdownTextFlowComponent
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

        public virtual MarkdownTextFlowContainer CreateTextFlow() => new MarkdownTextFlowContainer();

        public virtual SpriteText CreateSpriteText() => new SpriteText();

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
                case ThematicBreakBlock thematicBlock:
                    container.Add(CreateSeparator(thematicBlock));
                    break;
                case HeadingBlock headingBlock:
                    container.Add(CreateHeading(headingBlock));
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
                    var childContainer = CreateList(listBlock);
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
        /// Creates the visualiser for a <see cref="HeadingBlock"/>.
        /// </summary>
        /// <param name="headingBlock">The <see cref="HeadingBlock"/> to visualise.</param>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownHeading CreateHeading(HeadingBlock headingBlock) => new MarkdownHeading(headingBlock);

        /// <summary>
        /// Creates the visualiser for a <see cref="ParagraphBlock"/>.
        /// </summary>
        /// <param name="paragraphBlock">The <see cref="ParagraphBlock"/> to visualise.</param>
        /// <param name="level">The level in the document of <paramref name="paragraphBlock"/>.
        /// 0 for the root level, 1 for first-level items in a list, 2 for second-level items in a list, etc.</param>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownParagraph CreateParagraph(ParagraphBlock paragraphBlock, int level) => new MarkdownParagraph(paragraphBlock, level);

        /// <summary>
        /// Creates the visualiser for a <see cref="QuoteBlock"/>.
        /// </summary>
        /// <param name="quoteBlock">The <see cref="QuoteBlock"/> to visualise.</param>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownQuoteBlock CreateQuoteBlock(QuoteBlock quoteBlock) => new MarkdownQuoteBlock(quoteBlock);

        /// <summary>
        /// Creates the visualiser for a <see cref="FencedCodeBlock"/>.
        /// </summary>
        /// <param name="fencedCodeBlock">The <see cref="FencedCodeBlock"/> to visualise.</param>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownFencedCodeBlock CreateFencedCodeBlock(FencedCodeBlock fencedCodeBlock) => new MarkdownFencedCodeBlock(fencedCodeBlock);

        /// <summary>
        /// Creates the visualiser for a <see cref="Table"/>.
        /// </summary>
        /// <param name="table">The <see cref="Table"/> to visualise.</param>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownTable CreateTable(Table table) => new MarkdownTable(table);

        /// <summary>
        /// Creates the visualiser for a <see cref="ListBlock"/>.
        /// </summary>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownList CreateList(ListBlock listBlock) => new MarkdownList(listBlock);

        /// <summary>
        /// Creates the visualiser for a horizontal separator.
        /// </summary>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownSeparator CreateSeparator(ThematicBreakBlock thematicBlock) => new MarkdownSeparator(thematicBlock);

        /// <summary>
        /// Creates the visualiser for an element that isn't implemented.
        /// </summary>
        /// <param name="markdownObject">The <see cref="MarkdownObject"/> that isn't implemented.</param>
        /// <returns>The visualiser.</returns>
        protected virtual NotImplementedMarkdown CreateNotImplemented(IMarkdownObject markdownObject) => new NotImplementedMarkdown(markdownObject);

        protected virtual MarkdownPipeline CreateBuilder()
            => new MarkdownPipelineBuilder().UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                                            .UseEmojiAndSmiley()
                                            .UseAdvancedExtensions().Build();
    }
}
