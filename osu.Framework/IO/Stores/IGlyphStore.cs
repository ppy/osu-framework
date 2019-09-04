// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// Interface for a <see cref="IResourceStore{CharacterGlyph}"/>.
    /// </summary>
    public interface IGlyphStore : IResourceStore<CharacterGlyph>
    {
        /// <summary>
        /// Retrieves a <see cref="CharacterGlyph"/> that contains associated spacing information for a character.
        /// </summary>
        /// <param name="character">The character to retrieve the <see cref="CharacterGlyph"/> for.</param>
        /// <returns>The <see cref="CharacterGlyph"/> containing associated spacing information for <paramref name="character"/>.</returns>
        CharacterGlyph Get(char character);

        /// <summary>
        /// Retrieves the kerning for a pair of characters.
        /// </summary>
        /// <param name="left">The character to the left.</param>
        /// <param name="right">The character to the right.</param>
        /// <returns>The kerning.</returns>
        int GetKerning(char left, char right);
    }
}
