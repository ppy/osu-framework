// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Text
{
    /// <summary>
    /// A <see cref="ITexturedCharacterGlyph"/> provided as final output from a <see cref="TextBuilder"/>.
    /// </summary>
    public struct TextBuilderGlyph : ITexturedCharacterGlyph
    {
        public readonly Texture Texture => Glyph.Texture;
        public readonly FontMetrics? Metrics => Glyph.Metrics;
        public readonly float XOffset => ((fixedWidth - Glyph.Width) / 2 ?? Glyph.XOffset) * Size;
        public readonly float YOffset => Glyph.YOffset * Size;
        public readonly float XAdvance => (fixedWidth ?? Glyph.XAdvance) * Size;
        public readonly float Baseline => Glyph.Baseline * Size;
        public readonly float Width => Glyph.Width * Size;
        public readonly float Height => Glyph.Height * Size;
        public readonly char Character => Glyph.Character;

        public readonly ITexturedCharacterGlyph Glyph;

        /// <summary>
        /// The rectangle for the character to be drawn in.
        /// </summary>
        public RectangleF DrawRectangle { get; internal set; }

        /// <summary>
        /// Whether this is the first character on a new line.
        /// </summary>
        public bool OnNewLine { get; internal set; }

        /// <summary>
        /// The size to draw the glyph at.
        /// </summary>
        /// <remarks>
        /// For the same <see cref="FontUsage"/> size, this value can differ per-font depending on each font's metrics.
        /// </remarks>
        public readonly float Size;

        private readonly float? fixedWidth;

        internal TextBuilderGlyph(ITexturedCharacterGlyph glyph, float glyphSize, float? fixedWidth = null)
        {
            this = default;
            this.fixedWidth = fixedWidth;

            Glyph = glyph;
            Size = glyphSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetKerning<T>(T lastGlyph)
            where T : ICharacterGlyph
            => fixedWidth != null ? 0 : Glyph.GetKerning(lastGlyph);
    }
}
