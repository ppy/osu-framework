// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using osu.Framework.IO.Stores;

namespace osu.Framework.Text
{
    public sealed class CharacterGlyph : ICharacterGlyph
    {
        public float XOffset { get; }
        public float YOffset { get; }
        public float XAdvance { get; }
        public float Baseline { get; }
        public Grapheme Character { get; }

        /// <summary>
        /// The glyph store that contains this character.
        /// </summary>
        public IGlyphStore? ContainingStore { get; }

        public CharacterGlyph(Grapheme character, float xOffset, float yOffset, float xAdvance, float baseline, IGlyphStore? containingStore)
        {
            ContainingStore = containingStore;

            Character = character;
            XOffset = xOffset;
            YOffset = yOffset;
            XAdvance = xAdvance;
            Baseline = baseline;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetKerning<T>(T lastGlyph)
            where T : ICharacterGlyph
            => ContainingStore?.GetKerning(lastGlyph.Character, Character) ?? 0;
    }
}
