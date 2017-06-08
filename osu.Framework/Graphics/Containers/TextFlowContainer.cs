// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Text.RegularExpressions;
using System.Linq;
using System;
using osu.Framework.Caching;
using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using System.Diagnostics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable text object that supports more advanced text formatting.
    /// </summary>
    public class TextFlowContainer : FillFlowContainer
    {
        private float firstLineIndent;

        /// <summary>
        /// An indent value for the first (header) line of a paragraph.
        /// </summary>
        public float FirstLineIndent
        {
            get { return firstLineIndent; }
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
            get { return contentIndent; }
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
            get { return paragraphSpacing; }
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
            get { return lineSpacing; }
            set
            {
                if (value == lineSpacing) return;
                lineSpacing = value;

                layout.Invalidate();
            }
        }

        /// <summary>
        /// An easy way to set the full text of a text flow in one go.
        /// This will overwrite any existing text added using this method of <see cref="AddText(string, Action{SpriteText})"/>
        /// </summary>
        public string Text
        {
            set
            {
                Clear();
                AddText(value);
            }
        }

        public override bool HandleInput => false;

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.DrawSize) > 0)
                layout.Invalidate();
            return base.Invalidate(invalidation, source, shallPropagate);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layout.IsValid) layout.Refresh(computeLayout);
        }

        /// <summary>
        /// Add new text to this text flow. The \n character will create a new paragraph, not just a line break. If you need \n to be a line break, use <see cref="AddParagraph(string, Action{SpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of the <see cref="SpriteText" /> objects for each word created from the given text.</returns>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public IEnumerable<SpriteText> AddText(string text, Action<SpriteText> creationParameters = null) => addLine(new TextLine(text, creationParameters), true);

        /// <summary>
        /// Add a new paragraph to this text flow. The \n character will create a line break. If you need \n to be a new paragraph, not just a line break, use <see cref="AddText(string, Action{SpriteText})"/> instead.
        /// </summary>
        /// <returns>A collection of the <see cref="SpriteText" /> objects for each word created from the given text.</returns>
        /// <param name="paragraph">The paragraph to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new paragraph.</param>
        public IEnumerable<SpriteText> AddParagraph(string paragraph, Action<SpriteText> creationParameters = null) => addLine(new TextLine(paragraph, creationParameters), false);

        protected virtual SpriteText CreateSpriteText() => new SpriteText();

        private SpriteText createSpriteTextWithLine(TextLine line)
        {
            var spriteText = CreateSpriteText();
            line.ApplyParameters(spriteText);
            return spriteText;
        }

        public override void Add(Drawable drawable)
        {
            throw new InvalidOperationException($"Use {nameof(AddText)} to add text to a {nameof(TextFlowContainer)}.");
        }

        private float getLineHeight(Drawable d)
        {
            float? lineHeight = (d as SpriteText)?.TextSize ?? (d as NewLineContainer)?.HeightFactor;
            Trace.Assert(lineHeight != null, $"Height factor should never be null unless a child has an invalid type. Last child type={d.GetType()}.");
            return lineHeight.Value;
        }

        private IEnumerable<SpriteText> addLine(TextLine line, bool newLineIsParagraph)
        {
            bool first = true;
            var sprites = new List<SpriteText>();

            // !newLineIsParagraph effectively means that we want to add just *one* paragraph, which means we need to make sure that any previous paragraphs
            // are terminated. Thus, we add a NewLineContainer that indicates the end of the paragraph before adding our current paragraph.
            if (!newLineIsParagraph)
                base.Add(new NewLineContainer { HeightFactor = getLineHeight(Children.LastOrDefault()), IndicatesNewParagraph = true });

            foreach (string l in line.Text.Split('\n'))
            {
                if (!first)
                {
                    var lastChild = Children.LastOrDefault();
                    if (lastChild != null)
                        base.Add(new NewLineContainer { HeightFactor = getLineHeight(lastChild), IndicatesNewParagraph = newLineIsParagraph });
                }

                foreach (string word in Regex.Split(l, @"(?<=[ .,;-])"))
                {
                    if (string.IsNullOrEmpty(word)) continue;

                    var textSprite = createSpriteTextWithLine(line);
                    textSprite.Text = word;
                    sprites.Add(textSprite);
                    base.Add(textSprite);
                }

                first = false;
            }

            return sprites;
        }

        private Cached layout = new Cached();

        private void computeLayout()
        {
            var children = Children.ToList();

            bool first = true;

            Drawable previousC = null;
            float topMarginFactor = 0;
            foreach (var c in children)
            {
                NewLineContainer nlc = c as NewLineContainer;
                if (nlc != null)
                {
                    nlc.Height = nlc.IndicatesNewParagraph ? nlc.HeightFactor * paragraphSpacing : 0;
                }
                else if (first)
                {
                    c.Margin = new MarginPadding { Left = FirstLineIndent };
                    first = false;
                }
                else if (previousC is NewLineContainer || c.X == 0)
                {
                    topMarginFactor = getLineHeight(previousC);
                    c.Margin = new MarginPadding { Left = ContentIndent, Top = topMarginFactor * LineSpacing };
                }
                else
                    c.Margin = new MarginPadding { Top = topMarginFactor * LineSpacing };

                previousC = c;
            }
        }

        private class NewLineContainer : Container
        {
            public float HeightFactor;
            public bool IndicatesNewParagraph;

            public NewLineContainer()
            {
                RelativeSizeAxes = Axes.X;
            }
        }

        private class TextLine
        {
            public readonly string Text;
            private readonly Action<SpriteText> creationParameters;

            public TextLine(string text, Action<SpriteText> creationParameters = null)
            {
                Text = text;
                this.creationParameters = creationParameters;
            }

            public void ApplyParameters(SpriteText spriteText)
            {
                creationParameters?.Invoke(spriteText);
            }
        }
    }
}
