// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string which can be translated using a key lookup.
    /// </summary>
    public class TranslatableString : LocalisableFormattableString, IEquatable<TranslatableString>
    {
        public readonly string Key;

        /// <summary>
        /// Creates a <see cref="TranslatableString"/> using texts.
        /// </summary>
        /// <param name="key">The key for <see cref="LocalisationManager"/> to look up with.</param>
        /// <param name="fallback">The fallback string to use when no translation can be found.</param>
        /// <param name="args">Optional formattable arguments.</param>
        public TranslatableString(string key, string fallback, params object?[] args)
            : base(fallback, args)
        {
            Key = key;
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
            : base(interpolation)
        {
            Key = key;
        }

        protected override string FormatString(string fallback, object?[] args, LocalisationParameters parameters)
            => base.FormatString(parameters.Store?.Get(Key) ?? fallback, args, parameters);

        public bool Equals(TranslatableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && Key == other.Key;
        }

        public override bool Equals(ILocalisableStringData? other) => other is TranslatableString translatable && Equals(translatable);
        public override bool Equals(object? obj) => obj is TranslatableString translatable && Equals(translatable);

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Key);
    }
}
