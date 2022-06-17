// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;

namespace osu.Framework.Extensions.LocalisationExtensions
{
    public static class LocalisableStringExtensions
    {
        /// <summary>
        /// Returns a <see cref="LocalisableFormattableString"/> formatting the given <paramref name="value"/> to a string, along with an optional <paramref name="format"/> string.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format string.</param>
        public static LocalisableString ToLocalisableString(this IFormattable value, string? format = null) => LocalisableString.Format($"{{0:{format}}}", value);

        /// <summary>
        /// Returns a <see cref="CaseTransformableString"/> with the specified underlying localisable string uppercased.
        /// </summary>
        /// <param name="str">The localisable string.</param>
        /// <returns>A case transformable string with its localisable string uppercased.</returns>
        public static CaseTransformableString ToUpper(this LocalisableString str) => new CaseTransformableString(str, Casing.UpperCase);

        /// <summary>
        /// Returns a <see cref="CaseTransformableString"/> with the specified underlying string data uppercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A case transformable string with its string data uppercased.</returns>
        public static CaseTransformableString ToUpper(this ILocalisableStringData data) => new LocalisableString(data).ToUpper();

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying localisable string transformed to title case.
        /// </summary>
        /// <param name="str">The localisable string.</param>
        /// <returns>A case transformable string with its localisable string transformed to title case.</returns>
        public static CaseTransformableString ToTitle(this LocalisableString str) => new CaseTransformableString(str, Casing.TitleCase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data transformed to title case.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A case transformable string with its string data transformed to title case.</returns>
        public static CaseTransformableString ToTitle(this ILocalisableStringData data) => new LocalisableString(data).ToTitle();

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying localisable string lowercased.
        /// </summary>
        /// <param name="str">The localisable string.</param>
        /// <returns>A case transformable string with its localisable string lowercased.</returns>
        public static CaseTransformableString ToLower(this LocalisableString str) => new CaseTransformableString(str, Casing.LowerCase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data lowercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A case transformable string with its string data lowercased.</returns>
        public static CaseTransformableString ToLower(this ILocalisableStringData data) => new LocalisableString(data).ToLower();

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying localisable string transformed to sentence case.
        /// </summary>
        /// <param name="str">The localisable string.</param>
        /// <returns>A case transformable string with its localisable string transformed to sentence case.</returns>
        public static CaseTransformableString ToSentence(this LocalisableString str) => new CaseTransformableString(str, Casing.SentenceCase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data transformed to sentence case.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A case transformable string with its string data transformed to sentence case.</returns>
        public static CaseTransformableString ToSentence(this ILocalisableStringData data) => new LocalisableString(data).ToSentence();
    }
}
