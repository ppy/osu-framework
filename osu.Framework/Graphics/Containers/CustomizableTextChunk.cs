// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Implementation of <see cref="TextChunk{TSpriteText}"/> that support substitution of text placeholders for arbitrary placeholders
    /// as provided by <see cref="CustomizableTextContainer.TryGetIconFactory"/>.
    /// </summary>
    internal class CustomizableTextChunk<TSpriteText> : TextChunk<TSpriteText>
        where TSpriteText : SpriteText, new()
    {
        public CustomizableTextChunk(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, Action<TSpriteText>? creationParameters = null)
            : base(text, newLineIsParagraph, creationFunc, creationParameters)
        {
        }

        protected override IEnumerable<Drawable> CreateDrawablesFor(string text, TextFlowContainer textFlowContainer)
        {
            var customizableContainer = (CustomizableTextContainer)textFlowContainer;

            var sprites = new List<Drawable>();
            int index = 0;
            string str = text;

            while (index < str.Length)
            {
                Drawable? placeholderDrawable = null;
                int nextPlaceholderIndex = str.IndexOf(CustomizableTextContainer.UNESCAPED_LEFT, index, StringComparison.Ordinal);
                // make sure we skip ahead to the next [ as long as the current [ is escaped
                while (nextPlaceholderIndex != -1 && str.IndexOf(CustomizableTextContainer.ESCAPED_LEFT, nextPlaceholderIndex, StringComparison.Ordinal) == nextPlaceholderIndex)
                    nextPlaceholderIndex = str.IndexOf(CustomizableTextContainer.UNESCAPED_LEFT, nextPlaceholderIndex + 2, StringComparison.Ordinal);

                string? strPiece = null;

                if (nextPlaceholderIndex != -1)
                {
                    int placeholderEnd = str.IndexOf(CustomizableTextContainer.UNESCAPED_RIGHT, nextPlaceholderIndex, StringComparison.Ordinal);
                    // make sure we skip  ahead to the next ] as long as the current ] is escaped
                    while (placeholderEnd != -1 && str.IndexOf(CustomizableTextContainer.ESCAPED_RIGHT, placeholderEnd, StringComparison.InvariantCulture) == placeholderEnd)
                        placeholderEnd = str.IndexOf(CustomizableTextContainer.UNESCAPED_RIGHT, placeholderEnd + 2, StringComparison.Ordinal);

                    if (placeholderEnd != -1)
                    {
                        strPiece = str[index..nextPlaceholderIndex];
                        string placeholderStr = str.AsSpan(nextPlaceholderIndex + 1, placeholderEnd - nextPlaceholderIndex - 1).Trim().ToString();
                        string placeholderName = placeholderStr;
                        string paramStr = "";
                        int parensOpen = placeholderStr.IndexOf('(');

                        if (parensOpen != -1)
                        {
                            placeholderName = placeholderStr.AsSpan(0, parensOpen).Trim().ToString();
                            int parensClose = placeholderStr.IndexOf(')', parensOpen);
                            if (parensClose != -1)
                                paramStr = placeholderStr.AsSpan(parensOpen + 1, parensClose - parensOpen - 1).Trim().ToString();
                            else
                                throw new ArgumentException($"Missing ) in placeholder {placeholderStr}.");
                        }

                        if (int.TryParse(placeholderStr, out int placeholderIndex))
                        {
                            if (placeholderIndex < 0)
                                throw new ArgumentException($"Negative placeholder indices are invalid. Index {placeholderIndex} was used.");

                            placeholderDrawable = customizableContainer.Placeholders.ElementAtOrDefault(placeholderIndex);
                            if (placeholderDrawable == null)
                                throw new ArgumentException($"Placeholder with index {placeholderIndex} is null, or {placeholderIndex} is outside the bounds of allowable placeholder indices.");
                        }
                        else
                        {
                            object[] args;

                            if (string.IsNullOrWhiteSpace(paramStr))
                            {
                                args = Array.Empty<object>();
                            }
                            else
                            {
                                string[] argStrs = paramStr.Split(',');
                                args = new object[argStrs.Length];

                                for (int i = 0; i < argStrs.Length; ++i)
                                {
                                    if (!int.TryParse(argStrs[i], out int argVal))
                                        throw new ArgumentException($"The argument \"{argStrs[i]}\" in placeholder {placeholderStr} is not an integer.");

                                    args[i] = argVal;
                                }
                            }

                            if (!customizableContainer.TryGetIconFactory(placeholderName, out Delegate cb))
                                throw new ArgumentException($"There is no placeholder named {placeholderName}.");

                            placeholderDrawable = (Drawable?)cb.DynamicInvoke(args);
                        }

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

                if (placeholderDrawable != null)
                {
                    if (placeholderDrawable.Parent != null)
                        throw new ArgumentException("All icons used by a customizable text container must not have a parent. If you get this error message it means one of your icon factories created a drawable that was already added to another parent, or you used a drawable as a placeholder that already has another parent or you used an index-based placeholder (like [2]) more than once.");

                    sprites.Add(placeholderDrawable);
                }
            }

            return sprites;
        }
    }
}
