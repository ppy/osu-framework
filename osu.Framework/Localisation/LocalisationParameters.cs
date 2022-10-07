// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A set of parameters that control the way strings are localised.
    /// </summary>
    public class LocalisationParameters
    {
        /// <summary>
        /// The <see cref="ILocalisationStore"/> to be used for string lookups and culture-specific formatting.
        /// </summary>
        public readonly ILocalisationStore? Store;

        /// <summary>
        /// Whether to prefer the "original" script of <see cref="RomanisableString"/>s.
        /// </summary>
        public readonly bool PreferOriginalScript;

        /// <summary>
        /// Creates a new instance of <see cref="LocalisationParameters"/> based off another <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <param name="parameters">The <see cref="LocalisationParameters"/> to copy values from.</param>
        protected LocalisationParameters(LocalisationParameters parameters)
            : this(parameters.Store, parameters.PreferOriginalScript)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LocalisationParameters"/>.
        /// </summary>
        /// <param name="store">The <see cref="ILocalisationStore"/> to be used for string lookups and culture-specific formatting.</param>
        /// <param name="preferOriginalScript">Whether to prefer the "original" script of <see cref="RomanisableString"/>s.</param>
        public LocalisationParameters(ILocalisationStore? store, bool preferOriginalScript)
        {
            Store = store;
            PreferOriginalScript = preferOriginalScript;
        }
    }
}
