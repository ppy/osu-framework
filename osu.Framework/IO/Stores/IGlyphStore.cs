// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading.Tasks;
using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// Interface for a <see cref="IResourceStore{T}"/> of <see cref="CharacterGlyph"/>.
    /// </summary>
    public interface IGlyphStore : IResourceStore<CharacterGlyph>
    {
        /// <summary>
        /// The font's full name to be used for lookups.
        /// </summary>
        string FontName { get; }

        /// <summary>
        /// The font's baseline position, or <see langword="null"/> if not available (i.e. font not loaded or failed to load).
        /// </summary>
        float? Baseline { get; }

        /// <summary>
        /// Loads glyph information for consumption asynchronously.
        /// </summary>
        Task LoadFontAsync();

        /// <summary>
        /// Whether a glyph exists for the specified character in this store.
        /// </summary>
        bool HasGlyph(char c);

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
