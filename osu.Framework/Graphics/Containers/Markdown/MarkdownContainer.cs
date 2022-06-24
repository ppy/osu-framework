// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a markdown text document.
    /// </summary>
    public class MarkdownContainer : CompositeDrawable, IMarkdownTextComponent, IMarkdownTextFlowComponent
    {
        private const int root_level = 0;

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. <see cref="CompositeDrawable.InternalChildren"/>.
        /// Children's <see cref="Drawable.BypassAutoSizeAxes"/> are ignored for automatic sizing.
        /// Most notably, <see cref="Drawable.RelativePositionAxes"/> and <see cref="Drawable.RelativeSizeAxes"/> of children
        /// do not affect automatic sizing to avoid circular size dependencies.
        /// It is not allowed to manually set <see cref="Drawable.Size"/> (or <see cref="Drawable.Width"/> / <see cref="Drawable.Height"/>)
        /// on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set
            {
                if (value.HasFlagFast(Axes.X))
                    throw new ArgumentException($"{nameof(MarkdownContainer)} does not support an {nameof(AutoSizeAxes)} of {value}");

                base.AutoSizeAxes = value;
            }
        }

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

        private Uri documentUri;

        /// <summary>
        /// The URL of the loaded document.
        /// </summary>
        /// <exception cref="ArgumentException">If the provided URL was not a valid absolute URI.</exception>
        protected string DocumentUrl
        {
            get => documentUri?.AbsoluteUri;
            set
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    throw new ArgumentException($"Document URL ({value}) must be an absolute URI.");

                if (documentUri == uri)
                    return;

                documentUri = uri;

                contentCache.Invalidate();
            }
        }

        private Uri rootUri;

        /// <summary>
        /// The base URL for all root-relative links.
        /// </summary>
        /// <exception cref="ArgumentException">If the provided URL was not a valid absolute URI.</exception>
        protected string RootUrl
        {
            get => rootUri?.AbsoluteUri;
            set
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    throw new ArgumentException($"Root URL ({value}) must be an absolute URI.");

                if (rootUri == uri)
                    return;

                rootUri = uri;

                contentCache.Invalidate();
            }
        }

        private readonly Cached contentCache = new Cached();

        private readonly FillFlowContainer document;

        public MarkdownContainer()
        {
            InternalChild = document = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Direction = FillDirection.Vertical,
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
                string markdownText = Text;
                var pipeline = CreateBuilder();
                var parsed = Markdig.Markdown.Parse(markdownText, pipeline);

                // Turn all relative URIs in the document into absolute URIs
                foreach (var link in parsed.Descendants().OfType<LinkInline>())
                {
                    string url = link.Url;

                    if (string.IsNullOrEmpty(url))
                        continue;

                    if (!Validation.TryParseUri(url, out Uri linkUri))
                        continue;

                    if (linkUri.IsAbsoluteUri)
                        continue;

                    if (documentUri != null)
                    {
                        link.Url = rootUri != null && url.StartsWith('/')
                            // Ensure the URI is document-relative by removing all trailing slashes
                            ? new Uri(rootUri, new Uri(url.TrimStart('/'), UriKind.Relative)).AbsoluteUri
                            : new Uri(documentUri, new Uri(url, UriKind.Relative)).AbsoluteUri;
                    }
                }

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

                case HtmlBlock:
                    // HTML is not supported
                    break;

                case LinkReferenceDefinitionGroup:
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
        protected virtual MarkdownParagraph CreateParagraph(ParagraphBlock paragraphBlock, int level) => new MarkdownParagraph(paragraphBlock);

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
        protected virtual MarkdownList CreateList(ListBlock listBlock) => new MarkdownList();

        /// <summary>
        /// Creates the visualiser for a horizontal separator.
        /// </summary>
        /// <returns>The visualiser.</returns>
        protected virtual MarkdownSeparator CreateSeparator(ThematicBreakBlock thematicBlock) => new MarkdownSeparator();

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
