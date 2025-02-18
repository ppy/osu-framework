// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Implementation of <see cref="TextChunk{TSpriteText}"/> that support substitution of other <see cref="TextChunk{TSpriteText}"/>s for arbitrary placeholders.
    /// </summary>
    public class CompositeTextChunk<TSpriteText> : FormattableTextChunk<TSpriteText>
        where TSpriteText : SpriteText, new()
    {
        public readonly TextChunk<TSpriteText>[] Children;

        public CompositeTextChunk(LocalisableString text, bool newLineIsParagraph, Func<TSpriteText> creationFunc, TextChunk<TSpriteText>[] children, Action<TSpriteText>? creationParameters = null)
            : base(text, newLineIsParagraph, creationFunc, creationParameters)
        {
            Children = children;
        }

        protected override IEnumerable<Drawable>? GetDrawablesForSubstitution(string placeholder, TextFlowContainer textFlowContainer)
        {
            if (int.TryParse(placeholder, out int subpartIndex))
            {
                if (subpartIndex < 0)
                    throw new ArgumentException($"Negative indices are invalid. Index {subpartIndex} was used.");
                if (subpartIndex >= Children.Length)
                    throw new ArgumentException($"Index {subpartIndex} is outside the bounds of providen children text chunks.");

                var child = Children[subpartIndex];
                child.RecreateDrawablesFor(textFlowContainer);
                return child.Drawables;
            }

            throw new ArgumentException($"Index must be a number. {subpartIndex} was used.");
        }
    }
}
