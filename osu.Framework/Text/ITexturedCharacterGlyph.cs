// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Text
{
    /// <summary>
    /// A <see cref="ICharacterGlyph"/> with an associated <see cref="Texture"/>.
    /// </summary>
    public interface ITexturedCharacterGlyph : ICharacterGlyph
    {
        /// <summary>
        /// The texture for this character.
        /// </summary>
        Texture Texture { get; }

        /// <summary>
        /// The width of the area that should be drawn.
        /// </summary>
        float Width { get; }

        /// <summary>
        /// The height of the area that should be drawn.
        /// </summary>
        float Height { get; }

        /// <summary>
        /// Whether this character has a coloured texture, typically used for emoji.
        /// When true, the character will be rendered using its original texture colours.
        /// When false, rendered using the text colour.
        /// </summary>
        bool Coloured { get; }
    }

    public static class TexturedCharacterGlyphExtensions
    {
        /// <summary>
        /// Whether a <see cref="CharacterGlyph"/> represents a whitespace.
        /// </summary>
        public static bool IsWhiteSpace<T>(this T glyph)
            where T : ITexturedCharacterGlyph
            => glyph.Character.IsWhiteSpace();
    }
}
