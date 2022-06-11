// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Text
{
    public sealed class MultilineTextBuilder : TextBuilder
    {
        /// <inheritdoc />
        public MultilineTextBuilder(ITexturedGlyphLookupStore store, FontUsage font, float maxWidth, bool useFullGlyphHeight = true, Vector2 startOffset = default, Vector2 spacing = default,
                                    List<TextBuilderGlyph> characterList = null, char[] neverFixedWidthCharacters = null, char fallbackCharacter = '?', char fixedWidthReferenceCharacter = 'm')
            : base(store, font, maxWidth, useFullGlyphHeight, startOffset, spacing, characterList, neverFixedWidthCharacters, fallbackCharacter, fixedWidthReferenceCharacter)
        {
        }

        protected override void OnWidthExceeded() => AddNewLine();
    }
}
