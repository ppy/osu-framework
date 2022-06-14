// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Caching;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable text object that supports more advanced text formatting.
    /// </summary>
    public class TextFlowContainer : FillFlowContainer
    {
        private float firstLineIndent;
        private readonly Action<SpriteText> defaultCreationParameters;

        private readonly List<ITextPart> parts = new List<ITextPart>();

        private readonly Cached partsCache = new Cached();

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

                layout.Invalidate();
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

                layout.Invalidate();
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

                layout.Invalidate();
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

                layout.Invalidate();
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

                layout.Invalidate();
            }
        }

        /// <summary>
        /// An easy way to set the full text of a text flow in one go.
        /// This will overwrite any existing text added using this method of <see cref="AddText(LocalisableString, Action{SpriteText})"/>
        /// </summary>
        public LocalisableString Text
        {
            set
            {
                Clear();
                parts.Clear();

                AddText(value);
            }
        }

        [Resolved]
        internal LocalisationManager Localisation { get; private set; }

        private readonly Bindable<LocalisationParameters> localisationParameters = new Bindable<LocalisationParameters>();

        public TextFlowContainer(Action<SpriteText> defaultCreationParameters = null)
        {
            this.defaultCreationParameters = defaultCreationParameters;
        }

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

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
            layout.Invalidate();
        }

        protected override void Update()
        {
            base.Update();

            if (!partsCache.IsValid)
                RecreateAllParts();
        }

        public override IEnumerable<Drawable> FlowingChildren
        {
            get
            {
                if ((TextAnchor & (Anchor.x2 | Anchor.y2)) == 0)
                    return base.FlowingChildren;

                var childArray = base.FlowingChildren.ToArray();

                if ((TextAnchor & Anchor.x2) > 0)
                    reverseHorizontal(childArray);
                if ((TextAnchor & Anchor.y2) > 0)
                    reverseVertical(childArray);

                return childArray;
            }
        }

        protected override void UpdateAfterChildren()
        {
            if (!layout.IsValid)
            {
                computeLayout();
                layout.Validate();
            }

            base.UpdateAfterChildren();
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

        internal void ApplyDefaultCreationParamters(SpriteText spriteText) => defaultCreationParameters?.Invoke(spriteText);

        public override void Add(Drawable drawable)
        {
            throw new InvalidOperationException($"Use {nameof(AddText)} to add text to a {nameof(TextFlowContainer)}.");
        }

        public override void Clear(bool disposeChildren)
        {
            base.Clear(disposeChildren);
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
                RemoveRange(manualPart.Drawables);

            // make sure not to clear the list of parts by accident.
            base.Clear(true);

            foreach (var part in parts)
                recreatePart(part);

            partsCache.Validate();
        }

        private void recreatePart(ITextPart part)
        {
            part.RecreateDrawablesFor(this);
            foreach (var drawable in part.Drawables)
                base.Add(drawable);
        }

        private void reverseHorizontal(Drawable[] children)
        {
            int reverseStartIndex = 0;

            // Inverse the order of all children when displaying backwards, stopping at newline boundaries
            for (int i = 0; i < children.Length; i++)
            {
                if (!(children[i] is NewLineContainer))
                    continue;

                Array.Reverse(children, reverseStartIndex, i - reverseStartIndex);
                reverseStartIndex = i + 1;
            }

            // Extra loop for the last newline boundary (or all children if there are no newlines)
            Array.Reverse(children, reverseStartIndex, children.Length - reverseStartIndex);
        }

        private void reverseVertical(Drawable[] children)
        {
            // A vertical reverse reverses the order of the newline sections, but not the order within the newline sections
            // For code clarity this is done by reversing the entire array, and then reversing within the newline sections to restore horizontal order
            Array.Reverse(children);
            reverseHorizontal(children);
        }

        private readonly Cached layout = new Cached();

        private void computeLayout()
        {
            var childrenByLine = new List<List<Drawable>>();
            var curLine = new List<Drawable>();

            foreach (var c in Children)
            {
                c.Anchor = TextAnchor;
                c.Origin = TextAnchor;

                if (c is NewLineContainer nlc)
                {
                    curLine.Add(nlc);
                    childrenByLine.Add(curLine);
                    curLine = new List<Drawable>();
                }
                else
                {
                    if (c.X == 0)
                    {
                        if (curLine.Count > 0)
                            childrenByLine.Add(curLine);
                        curLine = new List<Drawable>();
                    }

                    curLine.Add(c);
                }
            }

            if (curLine.Count > 0)
                childrenByLine.Add(curLine);

            bool isFirstLine = true;
            float lastLineHeight = 0f;

            foreach (var line in childrenByLine)
            {
                bool isFirstChild = true;
                IEnumerable<float> lineBaseHeightValues = line.OfType<IHasLineBaseHeight>().Select(l => l.LineBaseHeight);
                float lineBaseHeight = lineBaseHeightValues.Any() ? lineBaseHeightValues.Max() : 0f;
                float currentLineHeight = 0f;
                float lineSpacingValue = lastLineHeight * LineSpacing;

                foreach (Drawable c in line)
                {
                    if (c is NewLineContainer nlc)
                    {
                        nlc.Height = nlc.IndicatesNewParagraph ? (currentLineHeight == 0 ? lastLineHeight : currentLineHeight) * ParagraphSpacing : 0;
                        continue;
                    }

                    float childLineBaseHeight = (c as IHasLineBaseHeight)?.LineBaseHeight ?? 0f;
                    MarginPadding margin = new MarginPadding { Top = (childLineBaseHeight != 0f ? lineBaseHeight - childLineBaseHeight : 0f) + lineSpacingValue };
                    if (isFirstLine)
                        margin.Left = FirstLineIndent;
                    else if (isFirstChild)
                        margin.Left = ContentIndent;

                    c.Margin = margin;

                    if (c.Height > currentLineHeight)
                        currentLineHeight = c.Height;

                    isFirstChild = false;
                }

                if (currentLineHeight != 0f)
                    lastLineHeight = currentLineHeight;

                isFirstLine = false;
            }
        }

        protected override bool ForceNewRow(Drawable child) => child is NewLineContainer;

        public class NewLineContainer : Container
        {
            public readonly bool IndicatesNewParagraph;

            public NewLineContainer(bool newParagraph)
            {
                IndicatesNewParagraph = newParagraph;
            }
        }
    }
}
