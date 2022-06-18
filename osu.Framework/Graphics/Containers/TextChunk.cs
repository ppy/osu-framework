// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Represents a plain chunk of text to be displayed in a text flow.
    /// </summary>
    public class TextChunk<TSpriteText> : TextPart
        where TSpriteText : SpriteText, new()
    {
        private readonly LocalisableString text;
        private readonly bool newLineIsParagraph;
        private readonly Func<TSpriteText> creationFunc;
        private readonly Action<TSpriteText>? creationParameters;

        public TextChunk(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, Action<TSpriteText>? creationParameters = null)
        {
            this.text = text;
            this.newLineIsParagraph = newLineIsParagraph;
            this.creationFunc = creationFunc;
            this.creationParameters = creationParameters;
        }

        protected override IEnumerable<Drawable> CreateDrawablesFor(TextFlowContainer textFlowContainer)
        {
            string currentContent = textFlowContainer.Localisation?.GetLocalisedString(text) ?? text.ToString();

            var drawables = new List<Drawable>();

            // !newLineIsParagraph effectively means that we want to add just *one* paragraph, which means we need to make sure that any previous paragraphs
            // are terminated. Thus, we add a NewLineContainer that indicates the end of the paragraph before adding our current paragraph.
            if (!newLineIsParagraph)
            {
                var newLine = new TextNewLine(true);
                newLine.RecreateDrawablesFor(textFlowContainer);
                drawables.AddRange(newLine.Drawables);
            }

            drawables.AddRange(CreateDrawablesFor(currentContent, textFlowContainer));
            return drawables;
        }

        protected virtual IEnumerable<Drawable> CreateDrawablesFor(string text, TextFlowContainer textFlowContainer)
        {
            bool first = true;
            var sprites = new List<Drawable>();

            foreach (string l in text.Split('\n'))
            {
                if (!first)
                {
                    Drawable? lastChild = sprites.LastOrDefault() ?? textFlowContainer.Children.LastOrDefault();

                    if (lastChild != null)
                    {
                        var newLine = new TextFlowContainer.NewLineContainer(newLineIsParagraph);
                        sprites.Add(newLine);
                    }
                }

                foreach (string word in SplitWords(l))
                {
                    if (string.IsNullOrEmpty(word)) continue;

                    var textSprite = CreateSpriteText(textFlowContainer);
                    textSprite.Text = word;
                    sprites.Add(textSprite);
                }

                first = false;
            }

            return sprites;
        }

        protected string[] SplitWords(string text)
        {
            var words = new List<string>();
            var builder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
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

        protected virtual TSpriteText CreateSpriteText(TextFlowContainer textFlowContainer)
        {
            var spriteText = creationFunc.Invoke();
            textFlowContainer.ApplyDefaultCreationParamters(spriteText);
            creationParameters?.Invoke(spriteText);
            return spriteText;
        }
    }
}
