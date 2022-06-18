// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Text
{
    public sealed class TexturedCharacterGlyph : ITexturedCharacterGlyph
    {
        public Texture Texture { get; }

        public float XOffset => glyph.XOffset * Scale;
        public float YOffset => glyph.YOffset * Scale;
        public float XAdvance => glyph.XAdvance * Scale;
        public float Baseline => glyph.Baseline * Scale;
        public char Character => glyph.Character;
        public float Width => Texture.Width * Scale;
        public float Height => Texture.Height * Scale;

        /// <summary>
        /// An adjustment factor in scale. This is applied to all other returned metric properties.
        /// </summary>
        public readonly float Scale;

        private readonly CharacterGlyph glyph;

        /// <summary>
        /// Create a new <see cref="TexturedCharacterGlyph"/> instance.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="scale">A scale factor to apply to exposed glyph metrics.</param>
        public TexturedCharacterGlyph(CharacterGlyph glyph, Texture texture, float scale = 1)
        {
            this.glyph = glyph;
            Scale = scale;
            Texture = texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetKerning<T>(T lastGlyph)
            where T : ICharacterGlyph
            => glyph.GetKerning(lastGlyph) * Scale;
    }
}
