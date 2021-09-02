﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Caching;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Graphics.Containers
{
    /// <inheritdoc />
    public class TextFlowContainer : TextFlowContainer<SpriteText>
    {
        public TextFlowContainer(Action<SpriteText> defaultCreationParameters = null)
            : base(defaultCreationParameters)
        {
        }

        protected override SpriteText CreateSpriteText() => new SpriteText();
    }

    /// <summary>
    /// A drawable text object that supports more advanced text formatting.
    /// </summary>
    public abstract class TextFlowContainer<T> : FillFlowContainer
        where T : SpriteText
    {
        private float firstLineIndent;
        private readonly Action<T> defaultCreationParameters;

        protected TextFlowContainer(Action<T> defaultCreationParameters = null)
        {
            this.defaultCreationParameters = defaultCreationParameters;
        }

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
        /// This will overwrite any existing text added using this method of <see cref="AddText(string, Action{T})"/>
        /// </summary>
        public string Text
        {
            set
            {
                Clear();
                AddText(value);
            }
        }

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
            layout.Invalidate();
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
        /// Add new text to this text flow. The \n character will create a new paragraph, not just a line break. If you need \n to be a line break, use <see cref="AddParagraph(string, Action{T})"/> instead.
        /// </summary>
        /// <returns>A collection of <see cref="Drawable" /> objects for each <see cref="SpriteText"/> word and <see cref="NewLineContainer"/> created from the given text.</returns>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public IEnumerable<Drawable> AddText(string text, Action<T> creationParameters = null) => AddLine(new TextChunk<T>(text, true, creationParameters));

        /// <summary>
        /// Add an arbitrary <see cref="SpriteText"/> to this <see cref="TextFlowContainer"/>.
        /// While default creation parameters are applied automatically, word wrapping is unavailable for contained words.
        /// This should only be used when a specialised <see cref="SpriteText"/> type is required.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public void AddText(T text, Action<T> creationParameters = null)
        {
            base.Add(text);
            defaultCreationParameters?.Invoke(text);
            creationParameters?.Invoke(text);
        }

        /// <summary>
        /// Add a new paragraph to this text flow. The \n character will create a line break. If you need \n to be a new paragraph, not just a line break, use <see cref="AddText(string, Action{T})"/> instead.
        /// </summary>
        /// <returns>A collection of <see cref="Drawable" /> objects for each <see cref="SpriteText"/> word and <see cref="NewLineContainer"/> created from the given text.</returns>
        /// <param name="paragraph">The paragraph to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new paragraph.</param>
        public IEnumerable<Drawable> AddParagraph(string paragraph, Action<T> creationParameters = null) => AddLine(new TextChunk<T>(paragraph, false, creationParameters));

        /// <summary>
        /// End current line and start a new one.
        /// </summary>
        public void NewLine() => base.Add(new NewLineContainer(false));

        /// <summary>
        /// End current paragraph and start a new one.
        /// </summary>
        public void NewParagraph() => base.Add(new NewLineContainer(true));

        protected abstract T CreateSpriteText();

        internal SpriteText CreateSpriteTextWithChunk(TextChunk<T> chunk)
        {
            var spriteText = CreateSpriteText();
            defaultCreationParameters?.Invoke(spriteText);
            chunk.ApplyParameters(spriteText);
            return spriteText;
        }

        public override void Add(Drawable drawable)
        {
            throw new InvalidOperationException($"Use {nameof(AddText)} to add text to a {nameof(TextFlowContainer)}.");
        }

        internal virtual IEnumerable<Drawable> AddLine(TextChunk<T> chunk)
        {
            var sprites = new List<Drawable>();

            // !newLineIsParagraph effectively means that we want to add just *one* paragraph, which means we need to make sure that any previous paragraphs
            // are terminated. Thus, we add a NewLineContainer that indicates the end of the paragraph before adding our current paragraph.
            if (!chunk.NewLineIsParagraph)
            {
                var newLine = new NewLineContainer(true);
                sprites.Add(newLine);
                base.Add(newLine);
            }

            sprites.AddRange(AddString(chunk));

            return sprites;
        }

        internal IEnumerable<Drawable> AddString(TextChunk<T> chunk)
        {
            bool first = true;
            var sprites = new List<Drawable>();

            foreach (string l in chunk.Text.Split('\n'))
            {
                if (!first)
                {
                    Drawable lastChild = Children.LastOrDefault();

                    if (lastChild != null)
                    {
                        var newLine = new NewLineContainer(chunk.NewLineIsParagraph);
                        sprites.Add(newLine);
                        base.Add(newLine);
                    }
                }

                foreach (string word in SplitWords(l))
                {
                    if (string.IsNullOrEmpty(word)) continue;

                    var textSprite = CreateSpriteTextWithChunk(chunk);
                    textSprite.Text = word;
                    sprites.Add(textSprite);
                    base.Add(textSprite);
                }

                first = false;
            }

            return sprites;
        }

        protected string[] SplitWords(string text)
        {
            var words = new List<string>();
            var builder = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                if (i == 0 || char.IsSeparator(text[i - 1]) || char.IsControl(text[i - 1]))
                {
                    words.Add(builder.ToString());
                    builder.Clear();
                }

                builder.Append(text[i]);
            }

            if (builder.Length > 0)
                words.Add(builder.ToString());

            return words.ToArray();
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
