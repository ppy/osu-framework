// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Caching;
using osu.Framework.Graphics.Sprites;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable text object that supports more advanced text formatting.
    /// </summary>
    public partial class TextFlowContainer : CompositeDrawable
    {
        private readonly Action<SpriteText> defaultCreationParameters;

        private readonly List<ITextPart> parts = new List<ITextPart>();

        private readonly Cached partsCache = new Cached();

        /// <summary>
        /// An indent value for the first (header) line of a paragraph.
        /// </summary>
        public float FirstLineIndent
        {
            get => Flow.FirstLineIndent;
            set => Flow.FirstLineIndent = value;
        }

        /// <summary>
        /// An indent value for all lines proceeding the first line in a paragraph.
        /// </summary>
        public float ContentIndent
        {
            get => Flow.ContentIndent;
            set => Flow.ContentIndent = value;
        }

        /// <summary>
        /// Vertical space between paragraphs (i.e. text separated by '\n') in multiples of the text size.
        /// The default value is 0.5.
        /// </summary>
        public float ParagraphSpacing
        {
            get => Flow.ParagraphSpacing;
            set => Flow.ParagraphSpacing = value;
        }

        /// <summary>
        /// Vertical space between lines both when a new paragraph begins and when line wrapping occurs.
        /// Additive with <see cref="ParagraphSpacing"/> on new paragraph. Default value is 0.
        /// </summary>
        public float LineSpacing
        {
            get => Flow.LineSpacing;
            set => Flow.LineSpacing = value;
        }

        /// <summary>
        /// The <see cref="Anchor"/> which text should flow from.
        /// </summary>
        public Anchor TextAnchor
        {
            get => Flow.TextAnchor;
            set => Flow.TextAnchor = value;
        }

        /// <summary>
        /// An easy way to set the full text of a text flow in one go.
        /// This will overwrite any existing text added using this method of <see cref="AddText(LocalisableString, Action{SpriteText})"/>
        /// </summary>
        public LocalisableString Text
        {
            set
            {
                Flow.Clear();
                parts.Clear();

                AddText(value);
            }
        }

        public new Axes RelativeSizeAxes
        {
            get => base.RelativeSizeAxes;
            set
            {
                base.RelativeSizeAxes = value;
                setFlowSizing();
            }
        }

        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set
            {
                base.AutoSizeAxes = value;
                setFlowSizing();
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                setFlowSizing();
            }
        }

        private void setFlowSizing()
        {
            // if the user has imposed `RelativeSizeAxes` or a fixed size on the X axis on the entire flow,
            // we want the child flow that actually does the layout here to match that.
            // however, the child flow must always be auto-sized in the Y axis
            // to correctly respect `TextAnchor`.
            if (AutoSizeAxes.HasFlagFast(Axes.X))
            {
                Flow.RelativeSizeAxes = Axes.None;
                Flow.AutoSizeAxes = Axes.Both;
            }
            else
            {
                Flow.AutoSizeAxes = Axes.Y;
                Flow.RelativeSizeAxes = Axes.X;
            }
        }

        public new MarginPadding Padding
        {
            get => base.Padding;
            set => base.Padding = value;
        }

        public Vector2 Spacing
        {
            get => Flow.Spacing;
            set => Flow.Spacing = value;
        }

        public Vector2 MaximumSize
        {
            get => Flow.MaximumSize;
            set => Flow.MaximumSize = value;
        }

        public new bool Masking
        {
            get => base.Masking;
            set => base.Masking = value;
        }

        public FillDirection Direction
        {
            get => Flow.Direction;
            set => Flow.Direction = value;
        }

        public IEnumerable<Drawable> Children => Flow.Children;

        [Resolved]
        internal LocalisationManager Localisation { get; private set; }

        protected readonly InnerFlow Flow;
        private readonly Bindable<LocalisationParameters> localisationParameters = new Bindable<LocalisationParameters>();

        public TextFlowContainer(Action<SpriteText> defaultCreationParameters = null)
        {
            this.defaultCreationParameters = defaultCreationParameters;

            InternalChild = Flow = CreateFlow().With(f => f.AutoSizeAxes = Axes.Both);
        }

        [Pure]
        protected virtual InnerFlow CreateFlow() => new InnerFlow();

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();

            localisationParameters.Value = Localisation.CurrentParameters.Value;
            RecreateAllParts();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            localisationParameters.BindValueChanged(_ => partsCache.Invalidate());
            ((IBindable<LocalisationParameters>)localisationParameters).BindTo(Localisation.CurrentParameters);
        }

        protected override void Update()
        {
            base.Update();

            if (!partsCache.IsValid)
                RecreateAllParts();
        }

        protected override int Compare(Drawable x, Drawable y)
        {
            // FillFlowContainer will reverse the ordering of right-anchored words such that the (previously) first word would be
            // the right-most word, whereas it should still be flowed left-to-right. This is achieved by reversing the comparator.
            if (TextAnchor.HasFlagFast(Anchor.x2))
                return base.Compare(y, x);

            return base.Compare(x, y);
        }

        /// <summary>
        /// Add new text to this text flow. The \n character will create a new paragraph, not just a line break.
        /// If you need \n to be a line break, use <see cref="AddParagraph{TSpriteText}(LocalisableString, Action{TSpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of <see cref="Drawable" /> objects for each <see cref="SpriteText"/> word and <see cref="NewLineContainer"/> created from the given text.</returns>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public ITextPart AddText<TSpriteText>(LocalisableString text, Action<TSpriteText> creationParameters = null)
            where TSpriteText : SpriteText, new()
            => AddPart(CreateChunkFor(text, true, () => new TSpriteText(), creationParameters));

        /// <inheritdoc cref="AddText{TSpriteText}(LocalisableString,Action{TSpriteText})"/>
        public ITextPart AddText(LocalisableString text, Action<SpriteText> creationParameters = null)
            => AddPart(CreateChunkFor(text, true, CreateSpriteText, creationParameters));

        /// <summary>
        /// Add an arbitrary <see cref="SpriteText"/> to this <see cref="TextFlowContainer"/>.
        /// While default creation parameters are applied automatically, word wrapping is unavailable for contained words.
        /// This should only be used when a specialised <see cref="SpriteText"/> type is required.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public void AddText<TSpriteText>(TSpriteText text, Action<TSpriteText> creationParameters = null)
            where TSpriteText : SpriteText
        {
            defaultCreationParameters?.Invoke(text);
            creationParameters?.Invoke(text);
            AddPart(new TextPartManual(text.Yield()));
        }

        /// <summary>
        /// Add a new paragraph to this text flow. The \n character will create a line break
        /// If you need \n to be a new paragraph, not just a line break, use <see cref="AddText{TSpriteText}(LocalisableString, Action{TSpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of <see cref="Drawable" /> objects for each <see cref="SpriteText"/> word and <see cref="NewLineContainer"/> created from the given text.</returns>
        /// <param name="paragraph">The paragraph to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new paragraph.</param>
        public ITextPart AddParagraph<TSpriteText>(LocalisableString paragraph, Action<TSpriteText> creationParameters = null)
            where TSpriteText : SpriteText, new()
            => AddPart(CreateChunkFor(paragraph, false, () => new TSpriteText(), creationParameters));

        /// <inheritdoc cref="AddParagraph{TSpriteText}(LocalisableString,Action{TSpriteText})"/>
        public ITextPart AddParagraph(LocalisableString paragraph, Action<SpriteText> creationParameters = null)
            => AddPart(CreateChunkFor(paragraph, false, CreateSpriteText, creationParameters));

        /// <summary>
        /// Creates an appropriate implementation of <see cref="TextChunk{TSpriteText}"/> for this text flow container type.
        /// </summary>
        protected internal virtual TextChunk<TSpriteText> CreateChunkFor<TSpriteText>(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, Action<TSpriteText> creationParameters = null)
            where TSpriteText : SpriteText, new()
            => new TextChunk<TSpriteText>(text, newLineIsParagraph, creationFunc, creationParameters);

        /// <summary>
        /// End current line and start a new one.
        /// </summary>
        public void NewLine() => AddPart(new TextNewLine(false));

        /// <summary>
        /// End current paragraph and start a new one.
        /// </summary>
        public void NewParagraph() => AddPart(new TextNewLine(true));

        protected internal virtual SpriteText CreateSpriteText() => new SpriteText();

        internal void ApplyDefaultCreationParameters(SpriteText spriteText) => defaultCreationParameters?.Invoke(spriteText);

        public void Clear(bool disposeChildren = true)
        {
            Flow.Clear(disposeChildren);
            parts.Clear();
        }

        /// <summary>
        /// Adds an <see cref="ITextPart"/> and its associated drawables to this <see cref="TextFlowContainer"/>.
        /// </summary>
        protected internal ITextPart AddPart(ITextPart part)
        {
            parts.Add(part);

            // if the parts cached is already invalid, there's no need to recreate the new addition. it will be created as part of the next validation.
            if (partsCache.IsValid)
                recreatePart(part);

            return part;
        }

        /// <summary>
        /// Removes an <see cref="ITextPart"/> from this text flow.
        /// </summary>
        /// <returns>Whether <see cref="ITextPart"/> was successfully removed from the flow.</returns>
        public bool RemovePart(ITextPart partToRemove)
        {
            if (!parts.Remove(partToRemove))
                return false;

            partsCache.Invalidate();
            return true;
        }

        protected virtual void RecreateAllParts()
        {
            // manual parts need to be manually removed before clearing contents,
            // to avoid accidentally disposing of them in the process.
            foreach (var manualPart in parts.OfType<TextPartManual>())
                Flow.RemoveRange(manualPart.Drawables, false);

            // make sure not to clear the list of parts by accident.
            Flow.Clear(true);

            foreach (var part in parts)
                recreatePart(part);

            partsCache.Validate();
        }

        private void recreatePart(ITextPart part)
        {
            part.RecreateDrawablesFor(this);
            foreach (var drawable in part.Drawables)
                Flow.Add(drawable);
        }

        protected partial class InnerFlow : FillFlowContainer
        {
            private float firstLineIndent;

            /// <summary>
            /// An indent value for the first (header) line of a paragraph.
            /// </summary>
            public float FirstLineIndent
            {
                get => firstLineIndent;
                set
                {
                    if (value == firstLineIndent) return;

                    firstLineIndent = value;

                    InvalidateLayout();
                }
            }

            private float contentIndent;

            /// <summary>
            /// An indent value for all lines proceeding the first line in a paragraph.
            /// </summary>
            public float ContentIndent
            {
                get => contentIndent;
                set
                {
                    if (value == contentIndent) return;

                    contentIndent = value;

                    InvalidateLayout();
                }
            }

            private float paragraphSpacing = 0.5f;

            /// <summary>
            /// Vertical space between paragraphs (i.e. text separated by '\n') in multiples of the text size.
            /// The default value is 0.5.
            /// </summary>
            public float ParagraphSpacing
            {
                get => paragraphSpacing;
                set
                {
                    if (value == paragraphSpacing) return;

                    paragraphSpacing = value;

                    InvalidateLayout();
                }
            }

            private float lineSpacing;

            /// <summary>
            /// Vertical space between lines both when a new paragraph begins and when line wrapping occurs.
            /// Additive with <see cref="ParagraphSpacing"/> on new paragraph. Default value is 0.
            /// </summary>
            public float LineSpacing
            {
                get => lineSpacing;
                set
                {
                    if (value == lineSpacing) return;

                    lineSpacing = value;

                    InvalidateLayout();
                }
            }

            private Anchor textAnchor = Anchor.TopLeft;

            /// <summary>
            /// The <see cref="Anchor"/> which text should flow from.
            /// </summary>
            public Anchor TextAnchor
            {
                get => textAnchor;
                set
                {
                    if (textAnchor == value)
                        return;

                    textAnchor = value;

                    Anchor = value;
                    Origin = value;

                    InvalidateLayout();
                }
            }

            protected override IEnumerable<Vector2> ComputeLayoutPositions()
            {
                // NOTE: This is a copy of `FillFlowContainer.ComputeLayoutPositions()`
                // with significant enough alterations to support all of the weird features that text flow wants to support
                // (like "bottom right anchor" that doesn't invert the order of the lines of text,
                // or like content indent, etc., etc.)
                // All differences will be highlighted via inline comments prefixed with DIFFERENCE:

                var max = MaximumSize;

                if (max == Vector2.Zero)
                {
                    var s = ChildSize;

                    // If we are autosize and haven't specified a maximum size, we should allow infinite expansion.
                    // If we are inheriting then we need to use the parent size (our ActualSize).
                    max.X = AutoSizeAxes.HasFlagFast(Axes.X) ? float.PositiveInfinity : s.X;
                    max.Y = AutoSizeAxes.HasFlagFast(Axes.Y) ? float.PositiveInfinity : s.Y;
                }

                var children = FlowingChildren.ToArray();
                if (children.Length == 0)
                    yield break;

                // The positions for each child we will return later on.
                var layoutPositions = ArrayPool<Vector2>.Shared.Rent(children.Length);

                // We need to keep track of row widths such that we can compute correct
                // positions for horizontal centre anchor children.
                // We also store for each child to which row it belongs.
                int[] rowIndices = ArrayPool<int>.Shared.Rent(children.Length);

                // DIFFERENCE: Contrary to `FillFlow` we care about the offset to the end of the flow (right side),
                // rather than to the middle.
                var rowWidths = new List<float> { 0 };
                // DIFFERENCE: We need to track "line base heights" as provided by `IHasLineBaseHeight`
                // for correct layouting of multiple font sizes on one line.
                var lineBaseHeights = new List<float> { 0 };

                // Variables keeping track of the current state while iterating over children
                // and computing initial flow positions.
                float rowHeight = 0;
                // DIFFERENCE: rowBeginOffset is missing because all children are presumed to be anchored and origined top-left.
                var current = Vector2.Zero;

                // First pass, computing initial flow positions
                Vector2 size = Vector2.Zero;

                // defer the return of the rented lists
                try
                {
                    for (int i = 0; i < children.Length; ++i)
                    {
                        Drawable c = children[i];

                        static Axes toAxes(FillDirection direction)
                        {
                            switch (direction)
                            {
                                case FillDirection.Full:
                                    return Axes.Both;

                                case FillDirection.Horizontal:
                                    return Axes.X;

                                case FillDirection.Vertical:
                                    return Axes.Y;

                                default:
                                    throw new ArgumentException($"{direction.ToString()} is not defined");
                            }
                        }

                        // In some cases (see the right hand side of the conditional) we want to permit relatively sized children
                        // in our fill direction; specifically, when children use FillMode.Fit to preserve the aspect ratio.
                        // Consider the following use case: A fill flow container has a fixed width but an automatic height, and fills
                        // in the vertical direction. Now, we can add relatively sized children with FillMode.Fit to make sure their
                        // aspect ratio is preserved while still allowing them to fill vertically. This special case can not result
                        // in an autosize-related feedback loop, and we can thus simply allow it.
                        if ((c.RelativeSizeAxes & AutoSizeAxes & toAxes(Direction)) != 0
                            && (c.FillMode != FillMode.Fit || c.RelativeSizeAxes != Axes.Both || c.Size.X > RelativeChildSize.X
                                || c.Size.Y > RelativeChildSize.Y || AutoSizeAxes == Axes.Both))
                        {
                            throw new InvalidOperationException(
                                "Drawables inside a fill flow container may not have a relative size axis that the fill flow container is filling in and auto sizing for. " +
                                $"The fill flow container is set to flow in the {Direction} direction and autosize in {AutoSizeAxes} axes and the child is set to relative size in {c.RelativeSizeAxes} axes.");
                        }

                        // DIFFERENCE: Disallow custom anchor/origin positions other than top left.
                        // Things will be accounted for accurately later.
                        // All calls to `spacingFactor()` in the original code thus reduce to returning (0,0).
                        if (c.RelativeAnchorPosition != Vector2.Zero)
                            throw new InvalidOperationException($"All drawables in a {nameof(TextFlowContainer)} must not specify custom {RelativeAnchorPosition}s. Only (0,0) is supported.");
                        if (c.RelativeOriginPosition != Vector2.Zero)
                            throw new InvalidOperationException($"All drawables in a {nameof(TextFlowContainer)} must not specify custom {RelativeOriginPosition}s. Only (0,0) is supported.");

                        // Populate running variables with sane initial values.
                        if (i == 0)
                        {
                            size = c.BoundingBox.Size;
                            // DIFFERENCE: Handle `ContentIndent` & `FirstLineIndent`.
                            // This only does the correct thing if the text is anchored to the left;
                            // right still could be made to work I guess, but what do you even do with centre?
                            current.X = ContentIndent + FirstLineIndent;
                        }

                        // DIFFERENCE: As per prior comments, `1 - spacingFactor(c).X` from the original code is just 1.
                        float rowWidth = current.X + size.X;

                        // We've exceeded our allowed width, move to a new row
                        // DIFFERENCE: `NewLineContainer`s also force a new row.
                        if (Direction != FillDirection.Horizontal && (Precision.DefinitelyBigger(rowWidth, max.X) || Direction == FillDirection.Vertical || c is NewLineContainer))
                        {
                            // DIFFERENCE: Handle `ContentIndent`. Only kinda correct for left text anchor.
                            current.X = ContentIndent;
                            // DIFFERENCE: Handle `LineSpacing`. This is deferred to the point of line break to avoid complicating the accounting.
                            current.Y += rowHeight * (1 + LineSpacing);

                            // DIFFERENCE: A new line also has an implicit height if it starts a new paragraph.
                            if (c is NewLineContainer nlc)
                                current.Y += nlc.IndicatesNewParagraph ? rowHeight * ParagraphSpacing : 0;

                            layoutPositions[i] = current;

                            // DIFFERENCE: `FillFlowContainer` does 0 here, because it's tracking offsets *to middle*, not total row widths.
                            rowWidths.Add(current.X + size.X);
                            // DIFFERENCE: `IHasLineBaseHeight` tracking.
                            lineBaseHeights.Add((c as IHasLineBaseHeight)?.LineBaseHeight ?? 0);

                            rowHeight = 0;
                        }
                        else
                        {
                            layoutPositions[i] = current;

                            // DIFFERENCE: Store width of the row, to be applied in case of non-left anchor
                            // in a second pass.
                            rowWidths[^1] = rowWidth;
                            // DIFFERENCE: `IHasLineBaseHeight` tracking.
                            lineBaseHeights[^1] = Math.Max(lineBaseHeights[^1], (c as IHasLineBaseHeight)?.LineBaseHeight ?? 0);
                        }

                        rowIndices[i] = rowWidths.Count - 1;
                        Vector2 stride = Vector2.Zero;

                        if (i < children.Length - 1)
                        {
                            // Compute stride.
                            // DIFFERENCE: All drawables are anchored top-left, so this reduces just to size.
                            stride = size;

                            c = children[i + 1];
                            size = c.BoundingBox.Size;

                            // DIFFERENCE: The original code did `stride += spacingFactor(c) * size`,
                            // but because all drawables here are anchored top-left, that's just zero.
                        }

                        stride += Spacing;

                        if (stride.Y > rowHeight)
                            rowHeight = stride.Y;
                        current.X += stride.X;
                    }

                    // DIFFERENCE: Original code did height accounting here to handle bottom / centre anchors.
                    // We don't need to do that. This flow's children will always be anchored top.

                    // DIFFERENCE: Second pass, adjusting the positions for text anchor & line base height.
                    if (!float.IsFinite(max.X))
                    {
                        float newMax = 0;
                        foreach (float rowWidth in rowWidths)
                            newMax = MathF.Max(newMax, rowWidth);
                        max.X = newMax;
                    }

                    for (int i = 0; i < children.Length; i++)
                    {
                        var c = children[i];

                        var layoutPosition = layoutPositions[i];

                        float rowOffsetToEnd = max.X - rowWidths[rowIndices[i]];

                        if (TextAnchor.HasFlagFast(Anchor.x1))
                            layoutPosition.X += rowOffsetToEnd / 2;
                        else if (TextAnchor.HasFlagFast(Anchor.x2))
                            layoutPosition.X += rowOffsetToEnd;

                        if (c is IHasLineBaseHeight hasLineBaseHeight)
                            layoutPosition.Y += lineBaseHeights[rowIndices[i]] - hasLineBaseHeight.LineBaseHeight;

                        yield return layoutPosition;
                    }
                }
                finally
                {
                    ArrayPool<Vector2>.Shared.Return(layoutPositions);
                    ArrayPool<int>.Shared.Return(rowIndices);
                }
            }
        }

        public partial class NewLineContainer : Container
        {
            public readonly bool IndicatesNewParagraph;

            public NewLineContainer(bool newParagraph)
            {
                IndicatesNewParagraph = newParagraph;
            }
        }
    }
}
