// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Implementation of <see cref="TextChunk{TSpriteText}"/> that support substitution of text placeholders for arbitrary placeholders
    /// as provided by <see cref="CustomizableTextContainer.TryGetIconFactory"/>.
    /// </summary>
    internal class CustomizableTextChunk<TSpriteText> : FormattableTextChunk<TSpriteText>
        where TSpriteText : SpriteText, new()
    {
        public CustomizableTextChunk(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, Action<TSpriteText>? creationParameters = null)
            : base(text, newLineIsParagraph, creationFunc, creationParameters)
        {
        }

        protected override Drawable[]? GetDrawablesForSubstitution(string placeholder, TextFlowContainer textFlowContainer)
        {
            var customizableContainer = (CustomizableTextContainer)textFlowContainer;

            Drawable? drawable;
            string placeholderName = placeholder;
            string paramStr = "";
            int parensOpen = placeholder.IndexOf('(');

            if (parensOpen != -1)
            {
                placeholderName = placeholder.AsSpan(0, parensOpen).Trim().ToString();
                int parensClose = placeholder.IndexOf(')', parensOpen);
                if (parensClose != -1)
                    paramStr = placeholder.AsSpan(parensOpen + 1, parensClose - parensOpen - 1).Trim().ToString();
                else
                    throw new ArgumentException($"Missing ) in placeholder {placeholder}.");
            }

            if (int.TryParse(placeholder, out int placeholderIndex))
            {
                if (placeholderIndex < 0)
                    throw new ArgumentException($"Negative placeholder indices are invalid. Index {placeholderIndex} was used.");

                drawable = customizableContainer.Placeholders.ElementAtOrDefault(placeholderIndex);
                if (drawable == null)
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
                            throw new ArgumentException($"The argument \"{argStrs[i]}\" in placeholder {placeholder} is not an integer.");

                        args[i] = argVal;
                    }
                }

                if (!customizableContainer.TryGetIconFactory(placeholderName, out Delegate? cb))
                    throw new ArgumentException($"There is no placeholder named {placeholderName}.");

                drawable = (Drawable?)cb.DynamicInvoke(args);
            }

            if (drawable == null) return null;

            if (drawable.Parent != null)
            {
                throw new ArgumentException(
                    "All icons used by a customizable text container must not have a parent. If you get this error message it means one of your icon factories created a drawable that was already added to another parent, or you used a drawable as a placeholder that already has another parent or you used an index-based placeholder (like [2]) more than once.");
            }

            return new[] { drawable };
        }
    }
}
