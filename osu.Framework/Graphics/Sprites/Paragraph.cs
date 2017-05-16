// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A drawable text object that supports word-wrapping amongst other paragraph-specific formatting.
    /// </summary>
    public class Paragraph : FillFlowContainer
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

        /// <summary>
        /// An easy way to set the full text of a paragraph in one go.
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
            layout.Invalidate();
            return base.Invalidate(invalidation, source, shallPropagate);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!layout.IsValid) layout.Refresh(computeLayout);
        }

        /// <summary>
        /// Add new text to this paragraph.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="creationParameters">A callback providing any <see cref="SpriteText" /> instances created for this new text.</param>
        public void AddText(string text, Action<SpriteText> creationParameters = null) => addLine(new TextLine(text, creationParameters));

        protected virtual SpriteText CreateSpriteText() => new SpriteText();

        private SpriteText createSpriteTextWithLine(TextLine line)
        {
            var spriteText = CreateSpriteText();
            line.ApplyParameters(spriteText);
            return spriteText;
        }

        private void addLine(TextLine line)
        {
            bool first = true;

            foreach (string l in line.Text.Split('\n'))
            {
                if (!first)
                {
                    var lastChild = Children.LastOrDefault();
                    if (lastChild != null)
                    {
                        var height = (lastChild as SpriteText)?.TextSize ?? lastChild.Height;

                        Add(new NewLineContainer { Height = height });
                    }
                }

                foreach (string word in Regex.Split(l, @"(?<=[ .,;-])"))
                {
                    if (string.IsNullOrEmpty(word)) continue;

                    var textSprite = createSpriteTextWithLine(line);
                    textSprite.Text = word;
                    Add(textSprite);
                }

                first = false;
            }
        }

        private Cached layout = new Cached();

        private void computeLayout()
        {
            var children = Children.ToList();

            bool newLineEncountered = false;
            bool first = true;

            foreach (var c in children)
            {
                if (c is NewLineContainer)
                    newLineEncountered = true;
                else if (first)
                {
                    c.Margin = new MarginPadding { Left = FirstLineIndent };
                    first = false;
                }
                else if (newLineEncountered || c.X == 0)
                {
                    c.Margin = new MarginPadding { Left = ContentIndent };
                    newLineEncountered = false;
                }
                else
                    c.Margin = new MarginPadding();
            }
        }

        private class NewLineContainer : Container
        {
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
