// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A set of parameters that control the way strings are localised.
    /// </summary>
    public class LocalisationParameters
    {
        public static readonly LocalisationParameters DEFAULT = new LocalisationParameters(null, false, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture);

        /// <summary>
        /// The <see cref="ILocalisationStore"/> to be used for string lookups.
        /// </summary>
        public readonly ILocalisationStore? Store;

        /// <summary>
        /// Whether to prefer the "original" script of <see cref="RomanisableString"/>s.
        /// </summary>
        public readonly bool PreferOriginalScript;

        /// <summary>
        /// Culture that is used for culture-specific string formatting, case transformations, etc.
        /// </summary>
        public readonly CultureInfo Culture;

        /// <summary>
        /// Culture that is used for language/string lookups.
        /// </summary>
        /// <remarks>
        /// Usually corresponds to <see cref="Store"/>.<see cref="ILocalisationStore.UICulture"/>.
        /// </remarks>
        public readonly CultureInfo UICulture;

        /// <summary>
        /// <see cref="IFormatProvider"/> that is used for culture-specific formatting of <see cref="LocalisableFormattableString"/>s.
        /// </summary>
        public readonly IFormatProvider FormatProvider;

        /// <summary>
        /// Creates a new instance of <see cref="LocalisationParameters"/> based off another <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <param name="parameters">The <see cref="LocalisationParameters"/> to copy values from.</param>
        protected LocalisationParameters(LocalisationParameters parameters)
            : this(parameters.Store, parameters.PreferOriginalScript, parameters.Culture, parameters.UICulture, parameters.FormatProvider)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <param name="store">The <see cref="ILocalisationStore"/> to be used for string lookups.</param>
        /// <param name="preferOriginalScript">Whether to prefer the "original" script of <see cref="RomanisableString"/>s.</param>
        /// <param name="culture">Culture that is used for culture-specific string formatting, case transformations, etc.</param>
        /// <param name="uiCulture">Culture that is used for language/string lookups.</param>
        /// <param name="formatProvider"><see cref="IFormatProvider"/> that is used for culture-specific formatting of <see cref="LocalisableFormattableString"/>s.</param>
        public LocalisationParameters(ILocalisationStore? store, bool preferOriginalScript, CultureInfo culture, CultureInfo uiCulture, IFormatProvider formatProvider)
        {
            Store = store;
            PreferOriginalScript = preferOriginalScript;
            Culture = culture;
            UICulture = uiCulture;
            FormatProvider = formatProvider;
        }

        /// <summary>
        /// Creates new <see cref="LocalisationParameters"/> from this <see cref="LocalisationParameters"/> with the provided fields changed.
        /// </summary>
        /// <returns>New <see cref="LocalisationParameters"/> based on this <see cref="LocalisationParameters"/>.</returns>
        public LocalisationParameters With(ILocalisationStore? store = null, bool? preferOriginalScript = null, CultureInfo? culture = null, CultureInfo? uiCulture = null,
                                           IFormatProvider? formatProvider = null)
            => new LocalisationParameters(
                store ?? Store,
                preferOriginalScript ?? PreferOriginalScript,
                culture ?? Culture,
                uiCulture ?? UICulture,
                formatProvider ?? FormatProvider
            );
    }
}
