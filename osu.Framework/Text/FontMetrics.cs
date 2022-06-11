// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Text
{
    /// <summary>
    /// Represents a structure containing the font's typographic metrics.
    /// </summary>
    // todo: convert to init-only properties on C# 9 to avoid always specifying parameter name during construction for context purposes.
    public readonly struct FontMetrics
    {
        /// <summary>
        /// The distance from the baseline to the highest ascender, representing the font's ascent.
        /// </summary>
        public float Ascent { get; }

        /// <summary>
        /// The distance from the baseline to the lowest descender, representing the font's descent.
        /// </summary>
        public float Descent { get; }

        /// <summary>
        /// The typographic line gap.
        /// </summary>
        public float LineGap { get; }

        /// <summary>
        /// The em size (i.e. UPM, units per em).
        /// </summary>
        public float EmSize { get; }

        /// <summary>
        /// Creates a new <see cref="FontMetrics"/>.
        /// </summary>
        /// <param name="ascent">The distance from the baseline to the highest ascender, representing the font's ascent.</param>
        /// <param name="descent">The distance from the baseline to the lowest descender, representing the font's descent.</param>
        /// <param name="emSize">The em size (i.e. UPM, units per em).</param>
        /// <param name="lineGap">The typographic line gap.</param>
        public FontMetrics(float ascent, float descent, float emSize, float lineGap = 0f)
        {
            Ascent = ascent;
            Descent = descent;
            LineGap = lineGap;
            EmSize = emSize;
        }

        /// <summary>
        /// Returns a scale computed from the font's metrics to apply to their respective glyphs,
        /// to display in a similar scale with other fonts that have different metrics.
        /// </summary>
        /// <seealso cref="FontUsage.CssScaling"/>
        public float GlyphScale => (Ascent + Descent + LineGap) / EmSize;
    }
}
