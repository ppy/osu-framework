// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Text
{
    public sealed class TexturedCharacterGlyph : ITexturedCharacterGlyph
    {
        public Texture Texture { get; }

        public float XOffset => glyph.XOffset * ScaleAdjustment;
        public float YOffset => glyph.YOffset * ScaleAdjustment;
        public float XAdvance => glyph.XAdvance * ScaleAdjustment;
        public char Character => glyph.Character;
        public float Width => Texture.Width * ScaleAdjustment;
        public float Height => Texture.Height * ScaleAdjustment;

        public readonly float ScaleAdjustment;
        private readonly CharacterGlyph glyph;

        public TexturedCharacterGlyph(CharacterGlyph glyph, Texture texture, float scaleAdjustment)
        {
            this.glyph = glyph;
            this.ScaleAdjustment = scaleAdjustment;

            Texture = texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetKerning<T>(T lastGlyph)
            where T : ICharacterGlyph
            => glyph.GetKerning(lastGlyph) * ScaleAdjustment;
    }
}
