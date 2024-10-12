// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Base class for implementations of <see cref="TextChunk{TSpriteText}"/> those support substitution for arbitrary drawables.
    /// </summary>
    /// <typeparam name="TSpriteText"></typeparam>
    public abstract class FormattableTextChunk<TSpriteText> : TextChunk<TSpriteText>
        where TSpriteText : SpriteText, new()
    {
        protected FormattableTextChunk(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, Action<TSpriteText>? creationParameters = null)
            : base(text, newLineIsParagraph, creationFunc, creationParameters)
        {
        }

        /// <summary>
        /// Creates drawables, which will be placed instead of placeholders.
        /// </summary>
        /// <param name="placeholder">Placeholder's text.</param>
        /// <param name="textFlowContainer">Container where created drawables will live.</param>
        protected abstract IEnumerable<Drawable>? GetDrawablesForSubstitution(string placeholder, TextFlowContainer textFlowContainer);

        protected sealed override IEnumerable<Drawable> CreateDrawablesFor(string text, TextFlowContainer textFlowContainer)
        {
            var sprites = new List<Drawable>();
            int index = 0;
            string str = text;

            while (index < str.Length)
            {
                IEnumerable<Drawable>? placeholderDrawables = null;
                int nextPlaceholderIndex = str.IndexOf(TextFlowContainer.UNESCAPED_LEFT, index, StringComparison.Ordinal);
                // make sure we skip ahead to the next [ as long as the current [ is escaped
                while (nextPlaceholderIndex != -1 && str.IndexOf(TextFlowContainer.ESCAPED_LEFT, nextPlaceholderIndex, StringComparison.Ordinal) == nextPlaceholderIndex)
                    nextPlaceholderIndex = str.IndexOf(TextFlowContainer.UNESCAPED_LEFT, nextPlaceholderIndex + 2, StringComparison.Ordinal);

                string? strPiece = null;

                if (nextPlaceholderIndex != -1)
                {
                    int placeholderEnd = str.IndexOf(TextFlowContainer.UNESCAPED_RIGHT, nextPlaceholderIndex, StringComparison.Ordinal);
                    // make sure we skip  ahead to the next ] as long as the current ] is escaped
                    while (placeholderEnd != -1 && str.IndexOf(TextFlowContainer.ESCAPED_RIGHT, placeholderEnd, StringComparison.InvariantCulture) == placeholderEnd)
                        placeholderEnd = str.IndexOf(TextFlowContainer.UNESCAPED_RIGHT, placeholderEnd + 2, StringComparison.Ordinal);

                    if (placeholderEnd != -1)
                    {
                        strPiece = str[index..nextPlaceholderIndex];
                        string placeholderStr = str.AsSpan(nextPlaceholderIndex + 1, placeholderEnd - nextPlaceholderIndex - 1).Trim().ToString();
                        placeholderDrawables = GetDrawablesForSubstitution(placeholderStr, textFlowContainer);

                        index = placeholderEnd + 1;
                    }
                }

                if (strPiece == null)
                {
                    strPiece = str.Substring(index);
                    index = str.Length;
                }

                // unescape stuff
                strPiece = CustomizableTextContainer.Unescape(strPiece);
                sprites.AddRange(base.CreateDrawablesFor(strPiece, textFlowContainer));

                if (placeholderDrawables != null)
                    sprites.AddRange(placeholderDrawables);
            }

            return sprites;
        }
    }
}
