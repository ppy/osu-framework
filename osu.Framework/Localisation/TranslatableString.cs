// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Globalization;
using System.Linq;
using osu.Framework.Logging;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string that can be translated with optional formattable arguments.
    /// </summary>
    public class TranslatableString : IEquatable<TranslatableString>, ILocalisableStringData
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

        public string GetLocalised(LocalisationParameters parameters)
        {
            if (parameters.Store == null)
                return ToString();

            string localisedFormat = parameters.Store.Get(Key) ?? Fallback;

            try
            {
                return string.Format(parameters.Store.EffectiveCulture, localisedFormat, Args.Select(argument =>
                {
                    if (argument is LocalisableString localisableString)
                        argument = localisableString.Data;

                    if (argument is ILocalisableStringData localisableData)
                        return localisableData.GetLocalised(parameters);

                    return argument;
                }).ToArray());
            }
            catch (FormatException e)
            {
                // The formatting has failed
                Logger.Log($"Localised format failed. Key: {Key}, culture: {parameters.Store.EffectiveCulture}, fallback format string: \"{Fallback}\", localised format string: \"{localisedFormat}\". Exception: {e}",
                    LoggingTarget.Runtime, LogLevel.Verbose);
            }

            return ToString();
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, Fallback, Args);

        public bool Equals(TranslatableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Key == other.Key
                   && Fallback == other.Fallback
                   && Args.SequenceEqual(other.Args);
        }

        public bool Equals(ILocalisableStringData? other) => other is TranslatableString translatable && Equals(translatable);
        public override bool Equals(object? obj) => obj is TranslatableString translatable && Equals(translatable);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Key);
            hashCode.Add(Fallback);
            foreach (object? arg in Args)
                hashCode.Add(arg);
            return hashCode.ToHashCode();
        }
    }
}
