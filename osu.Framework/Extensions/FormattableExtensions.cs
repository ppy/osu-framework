// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Extensions
{
    public static class FormattableExtensions
    {
        /// <summary>
        /// Formats the value of <paramref name="formattable"/> using the default format string and the supplied <paramref name="formatProvider"/>.
        /// </summary>
        public static string ToString(this IFormattable formattable, IFormatProvider formatProvider) => formattable.ToString(null, formatProvider);
    }
}
