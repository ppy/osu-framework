// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Framework.Extensions.LocalisationExtensions
{
    public static class LocalisableStringExtensions
    {
        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data uppercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A localisable string with its string data uppercased.</returns>
        public static LocalisableString ToUpper(this ILocalisableStringData data) => new LocalisableString(data, Casing.Uppercase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data transformed to title case.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A localisable string with its string data transformed to title case.</returns>
        public static LocalisableString ToTitle(this ILocalisableStringData data) => new LocalisableString(data, Casing.TitleCase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data lowercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A localisable string with its string data lowercased.</returns>
        public static LocalisableString ToLower(this ILocalisableStringData data) => new LocalisableString(data, Casing.Lowercase);
    }
}
