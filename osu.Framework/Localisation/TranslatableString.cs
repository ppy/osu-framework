// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Globalization;
using osu.Framework.Logging;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string that can be translated with optional formattable arguments.
    /// </summary>
    public class TranslatableString
    {
        public readonly string Key;
        public readonly string Fallback;
        public readonly object?[] Args;

        /// <summary>
        /// Creates a <see cref="TranslatableString"/> using texts.
        /// </summary>
        /// <param name="key">The key for <see cref="LocalisationManager"/> to look up with.</param>
        /// <param name="fallback">The fallback string to use when no translation can be found.</param>
        /// <param name="args">Optional formattable arguments.</param>
        public TranslatableString(string key, string fallback, params object?[] args)
        {
            Key = key;
            Fallback = fallback;
            Args = args;
        }

        /// <summary>
        /// Creates a <see cref="TranslatableString"/> using interpolated string.
        /// Example usage:
        /// <code>
        /// new TranslatableString("played_count_self", $"You have played {count:N0} times!");
        /// </code>
        /// </summary>
        /// <param name="key">The key for <see cref="LocalisationManager"/> to look up with.</param>
        /// <param name="interpolation">The interpolated string containing fallback and formattable arguments.</param>
        public TranslatableString(string key, FormattableString interpolation)
        {
            Key = key;
            Fallback = interpolation.Format;
            Args = interpolation.GetArguments();
        }

        public string Format(ILocalisationStore store)
        {
            var localisedFormat = store.Get(Key);

            if (localisedFormat == null) return ToString();

            try
            {
                return string.Format(store.EffectiveCulture, localisedFormat, Args);
            }
            catch (FormatException e)
            {
                // The formatting has failed
                Logger.Log($"Localised format failed. Key: {Key}, culture: {store.EffectiveCulture.Name}, fallback format string: \"{Fallback}\", localised format string: \"{localisedFormat}\". Exception: {e}",
                    LoggingTarget.Runtime, LogLevel.Verbose);
            }

            return ToString();
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, Fallback, Args);
    }
}
