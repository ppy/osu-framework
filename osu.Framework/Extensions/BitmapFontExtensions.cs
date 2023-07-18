// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using SharpFNT;

namespace osu.Framework.Extensions
{
    /// <summary>
    /// Extensions that provide equivalent functions to ones in <see cref="BitmapFont"/>,
    /// but use <see cref="Rune"/> parameters instead of <c>char</c>.
    /// </summary>
    /// <remarks>
    /// Code adapted from https://github.com/AuroraBertaOldham/SharpFNT/blob/a7f11fde1deef4821cd4e56368fa173813dade31/SharpFNT/BitmapFont.cs.
    /// </remarks>
    public static class BitmapFontExtensions
    {
        /// <summary>
        /// Equivalent to <see cref="BitmapFont.GetKerningAmount"/>.
        /// </summary>
        public static int GetKerningAmount(this BitmapFont font, Rune left, Rune right)
        {
            if (font.KerningPairs == null)
                return 0;

            return font.KerningPairs.TryGetValue(new KerningPair(left.Value, right.Value), out int kerningValue) ? kerningValue : 0;
        }

        /// <summary>
        /// Equivalent to <see cref="BitmapFont.GetCharacter"/>.
        /// </summary>
        public static Character? GetCharacter(this BitmapFont font, Rune rune, bool tryInvalid = true)
        {
            if (font.Characters == null)
                return null;

            if (font.Characters.TryGetValue(rune.Value, out var result))
                return result;

            if (tryInvalid && font.Characters.TryGetValue(-1, out result))
                return result;

            return null;
        }
    }
}
