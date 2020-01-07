// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using osu.Framework.Configuration;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A descriptor representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisableString : IEquatable<LocalisableString>
    {
        // Reuse string1 and string2 in different types to save size.
        // TODO: this is a good candidate of discriminated union (https://github.com/dotnet/csharplang/issues/113).
        private readonly StringLocalisationType type;
        private readonly string string1;
        private readonly string? string2;
        private readonly object?[]? args;

        public static implicit operator LocalisableString(string text) => new LocalisableString(StringLocalisationType.None, text, null, null);

        public override string ToString()
            => type == StringLocalisationType.Translation
                ? string.Format(CultureInfo.InvariantCulture, string1, args!)
                : (string1 ?? string.Empty);

        public bool Equals(LocalisableString other) =>
            type == other.type &&
            string1 == other.string1 &&
            string2 == other.string2 &&
            ReferenceEquals(args, other.args); // SequenceEqual cannot handle equality of boxed value types. Equality arg by arg isn't worthy.

        public bool TryGetPlainText([NotNullWhen(true)] out string? text)
        {
            if (type == StringLocalisationType.None)
            {
                text = string1 ?? string.Empty;
                return true;
            }
            else
            {
                text = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>. This localises based on the value of <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        /// <param name="romanised">The romanised string.</param>
        /// <param name="unicode">The unicode string.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableString FromRomanisation(string romanised, string? unicode = null)
            => new LocalisableString(StringLocalisationType.Romanisation, romanised, unicode, null);

        public bool TryGetRomanisation([NotNullWhen(true)] out string? romanised, out string? unicode)
        {
            if (type == StringLocalisationType.Romanisation)
            {
                romanised = string1;
                unicode = string2;
                return true;
            }
            else
            {
                romanised = null;
                unicode = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// Supposed usage:
        /// <code>
        /// LocalisableStringDescriptor.FromTranslatable("Played_count_self", "You have played {0:N0} times!", count);
        /// </code>
        /// </summary>
        /// <param name="key">The key to look for localisation by <see cref="LocalisationManager"/>.</param>
        /// <param name="fallback">The fallback text to use when localised text is not found.</param>
        /// <param name="args">The arguments to format the text with.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableString FromTranslatable(string key, string fallback, params object[] args)
            => new LocalisableString(StringLocalisationType.Translation, fallback, key, args);

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// Supposed usage:
        /// <code>
        /// LocalisableStringDescriptor.FromInterpolatedTranslatable($"You have played {count:N0} times!", "Played_count_self");
        /// </code>
        /// </summary>
        /// <param name="interpolated">The interpolation string containing fallback and arguments.</param>
        /// <param name="key">The key to look for localisation by <see cref="LocalisationManager"/>.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableString FromInterpolatedTranslatable(FormattableString interpolated, string key)
            => new LocalisableString(StringLocalisationType.Translation, interpolated.Format, key, interpolated.GetArguments());

        public bool TryGetTranslatable([NotNullWhen(true)] out string? key, [NotNullWhen(true)] out string? fallback, [NotNullWhen(true)] out object?[]? args)
        {
            if (type == StringLocalisationType.Translation)
            {
                key = string2;
                fallback = string1;
                args = this.args;
                return true;
            }
            else
            {
                key = null;
                fallback = null;
                args = null;
                return false;
            }
        }

        private LocalisableString(StringLocalisationType type, string string1, string? string2, object?[]? args)
        {
            this.type = type;
            this.string1 = string1;
            this.string2 = string2;
            this.args = args;
        }

        /// <summary>
        /// Represents the type of a <see cref="LocalisableString"/>.
        /// </summary>
        private enum StringLocalisationType
        {
            /// <summary>
            /// The <see cref="LocalisableString"/> is constructed from plain text and not localisable.
            /// </summary>
            None,

            /// <summary>
            /// The <see cref="LocalisableString"/> is constructed from Romanisation/Unicode pair.
            /// </summary>
            Romanisation,

            /// <summary>
            /// The <see cref="LocalisableString"/> is constructed from translation key.
            /// </summary>
            Translation,
        }
    }
}
