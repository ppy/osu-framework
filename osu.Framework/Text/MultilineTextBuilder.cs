// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Text
{
    public sealed class MultilineTextBuilder : TextBuilder
    {
        /// <summary>
        /// Creates a new <see cref="TextBuilder"/>.
        /// </summary>
        /// <param name="store">The store from which glyphs are to be retrieved from.</param>
        /// <param name="font">The font to use for glyph lookups from <paramref name="store"/>.</param>
        /// <param name="maxWidth">The maximum width of the resulting text bounds.</param>
        /// <param name="useFontSizeAsHeight">True to use the provided <paramref name="font"/> size as the height for each line. False if the height of each individual glyph should be used.</param>
        /// <param name="startOffset">The offset at which characters should begin being added at.</param>
        /// <param name="spacing">The spacing between characters.</param>
        /// <param name="characterList">That list to contain all resulting <see cref="TextBuilderGlyph"/>s.</param>
        /// <param name="neverFixedWidthCharacters">The characters for which fixed width should never be applied.</param>
        /// <param name="fallbackCharacter">The character to use if a glyph lookup fails.</param>
        /// <param name="fixedWidthReferenceCharacter">The character to use to calculate the fixed width width. Defaults to 'm'.</param>
        public MultilineTextBuilder(ITexturedGlyphLookupStore store, FontUsage font, float maxWidth, bool useFontSizeAsHeight = true, Vector2 startOffset = default, Vector2 spacing = default,
                                    List<TextBuilderGlyph> characterList = null, char[] neverFixedWidthCharacters = null, char fallbackCharacter = '?', char fixedWidthReferenceCharacter = 'm')
            : base(store, font, maxWidth, useFontSizeAsHeight, startOffset, spacing, characterList, neverFixedWidthCharacters, fallbackCharacter, fixedWidthReferenceCharacter)
        {
        }

        protected override void OnWidthExceeded() => AddNewLine();
    }
}
