// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Text
{
    public sealed class TruncatingTextBuilder : TextBuilder
    {
        private readonly char[] neverFixedWidthCharacters;
        private readonly char fallbackCharacter;
        private readonly ITexturedGlyphLookupStore store;
        private readonly FontUsage font;
        private readonly string ellipsisString;
        private readonly bool useFontSizeAsHeight;
        private readonly Vector2 spacing;

        private bool ellipsisAdded;
        private bool addingEllipsis; // Only used temporarily during the addition of the ellipsis.

        /// <summary>
        /// Creates a new <see cref="TextBuilder"/>.
        /// </summary>
        /// <param name="store">The store from which glyphs are to be retrieved from.</param>
        /// <param name="font">The font to use for glyph lookups from <paramref name="store"/>.</param>
        /// <param name="ellipsisString">The string to be displayed if the text exceeds the allowable text area.</param>
        /// <param name="useFontSizeAsHeight">True to use the provided <see cref="font"/> size as the height for each line. False if the height of each individual glyph should be used.</param>
        /// <param name="startOffset">The offset at which characters should begin being added at.</param>
        /// <param name="spacing">The spacing between characters.</param>
        /// <param name="maxWidth">The maximum width of the resulting text bounds.</param>
        /// <param name="characterList">That list to contain all resulting <see cref="TextBuilderGlyph"/>s.</param>
        /// <param name="neverFixedWidthCharacters">The characters for which fixed width should never be applied.</param>
        /// <param name="fallbackCharacter">The character to use if a glyph lookup fails.</param>
        /// <param name="fixedWidthReferenceCharacter">The character to use to calculate the fixed width width. Defaults to 'm'.</param>
        public TruncatingTextBuilder(ITexturedGlyphLookupStore store, FontUsage font, float maxWidth, string ellipsisString = null, bool useFontSizeAsHeight = true, Vector2 startOffset = default,
                                     Vector2 spacing = default, List<TextBuilderGlyph> characterList = null, char[] neverFixedWidthCharacters = null, char fallbackCharacter = '?', char fixedWidthReferenceCharacter = 'm')
            : base(store, font, maxWidth, useFontSizeAsHeight, startOffset, spacing, characterList, neverFixedWidthCharacters, fallbackCharacter, fixedWidthReferenceCharacter)
        {
            this.store = store;
            this.font = font;
            this.ellipsisString = ellipsisString;
            this.useFontSizeAsHeight = useFontSizeAsHeight;
            this.spacing = spacing;
            this.neverFixedWidthCharacters = neverFixedWidthCharacters;
            this.fallbackCharacter = fallbackCharacter;
        }

        public override void Reset()
        {
            base.Reset();

            ellipsisAdded = false;
        }

        protected override bool CanAddCharacters => (base.CanAddCharacters && !ellipsisAdded) || addingEllipsis;

        protected override bool HasAvailableSpace(float length) => base.HasAvailableSpace(length) || addingEllipsis;

        protected override void OnWidthExceeded()
        {
            if (addingEllipsis)
                return;

            addingEllipsis = true;

            try
            {
                if (string.IsNullOrEmpty(ellipsisString))
                    return;

                // Characters is re-used to reduce allocations, but must be reset after use
                int startIndex = Characters.Count;

                // Compute the ellipsis to find out the size required
                var builder = new TextBuilder(store, font, float.MaxValue, useFontSizeAsHeight, Vector2.Zero, spacing, Characters, neverFixedWidthCharacters, fallbackCharacter);
                builder.AddText(ellipsisString);

                float ellipsisWidth = builder.Bounds.X;
                TextBuilderGlyph firstEllipsisGlyph = builder.Characters[startIndex];

                // Reset the characters list by removing all ellipsis characters
                Characters.RemoveRange(startIndex, Characters.Count - startIndex);

                while (true)
                {
                    RemoveLastCharacter();

                    if (Characters.Count == 0)
                        break;

                    if (Characters[^1].IsWhiteSpace())
                        continue;

                    if (base.HasAvailableSpace(firstEllipsisGlyph.GetKerning(Characters[^1]) + spacing.X + ellipsisWidth))
                        break;
                }

                AddText(ellipsisString);
            }
            finally
            {
                addingEllipsis = false;
                ellipsisAdded = true;
            }
        }
    }
}
