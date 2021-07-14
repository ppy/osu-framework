// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Framework.Extensions.LocalisationExtensions
{
    public static class LocalisableStringExtensions
    {
        /// <summary>
        /// Returns a <see cref="TransformableString"/> with the specified underlying string data uppercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A transformable string with its string data uppercased.</returns>
        public static TransformableString ToUpper(this ILocalisableStringData data) => new TransformableString(data, Casing.Uppercase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data transformed to title case.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A transformable string with its string data transformed to title case.</returns>
        public static TransformableString ToTitle(this ILocalisableStringData data) => new TransformableString(data, Casing.TitleCase);

        /// <summary>
        /// Returns a <see cref="LocalisableString"/> with the specified underlying string data lowercased.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A transformable string with its string data lowercased.</returns>
        public static TransformableString ToLower(this ILocalisableStringData data) => new TransformableString(data, Casing.Lowercase);
    }
}
