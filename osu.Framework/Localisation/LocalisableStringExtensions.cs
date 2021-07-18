// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Localisation
{
    public static class LocalisableStringExtensions
    {
        /// <summary>
        /// Returns a <see cref="LocalisableFormattableString"/> formatting the given <paramref name="value"/> with the specified <paramref name="format"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format string.</param>
        public static LocalisableFormattableString ToLocalisableString(this IFormattable value, string format) => new LocalisableFormattableString(value, format);
    }
}
