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
    public readonly struct LocalisableStringDescriptor : IEquatable<LocalisableStringDescriptor>
    {
        // Reuse string1 and string2 in different types to save size.
        // TODO: this is a good candidate of discriminated union (https://github.com/dotnet/csharplang/issues/113).
        private readonly StringLocalisationType type;
        private readonly string string1;
        private readonly string? string2;
        private readonly object?[]? args;

        public static implicit operator LocalisableStringDescriptor(string text) => new LocalisableStringDescriptor(StringLocalisationType.None, text, null, null);

        public override string ToString()
            => type == StringLocalisationType.Translation
                ? string.Format(CultureInfo.InvariantCulture, string1, args!)
                : (string1 ?? string.Empty);

        public bool Equals(LocalisableStringDescriptor other) =>
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
        /// Creates a new <see cref="LocalisableStringDescriptor"/>. This localises based on the value of <see cref="FrameworkSetting.ShowUnicode"/>.
        /// </summary>
        /// <param name="romanized">The romanised string.</param>
        /// <param name="unicode">The unicode string.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableStringDescriptor FromRomanization(string romanized, string? unicode = null)
            => new LocalisableStringDescriptor(StringLocalisationType.Romanization, romanized, unicode, null);

        public bool TryGetRomanization([NotNullWhen(true)] out string? romanized, out string? unicode)
        {
            if (type == StringLocalisationType.Romanization)
            {
                romanized = string1;
                unicode = string2;
                return true;
            }
            else
            {
                romanized = null;
                unicode = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableStringDescriptor"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// Supposed usage:
        /// <code>
        /// LocalisableStringDescriptor.FromTranslatable("Played_count_self", "You have played {0:N0} times!", count);
        /// </code>
        /// </summary>
        /// <param name="key">The key to look for localisation by <see cref="LocalisationManager"/>.</param>
        /// <param name="fallback">The fallback text to use when localised text is not found.</param>
        /// <param name="args">The arguments to format the text with.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableStringDescriptor FromTranslatable(string key, string fallback, params object[] args)
            => new LocalisableStringDescriptor(StringLocalisationType.Translation, fallback, key, args);

        /// <summary>
        /// Creates a new <see cref="LocalisableStringDescriptor"/>. This localises based on the value of <see cref="FrameworkSetting.Locale"/>.
        /// Supposed usage:
        /// <code>
        /// LocalisableStringDescriptor.FromInterpolatedTranslatable($"You have played {count:N0} times!", "Played_count_self");
        /// </code>
        /// </summary>
        /// <param name="interpolated">The interpolation string containing fallback and arguments.</param>
        /// <param name="key">The key to look for localisation by <see cref="LocalisationManager"/>.</param>
        /// <returns>The descriptor of the localisable string.</returns>
        public static LocalisableStringDescriptor FromInterpolatedTranslatable(FormattableString interpolated, string key)
            => new LocalisableStringDescriptor(StringLocalisationType.Translation, interpolated.Format, key, interpolated.GetArguments());

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

        private LocalisableStringDescriptor(StringLocalisationType type, string string1, string? string2, object?[]? args)
        {
            this.type = type;
            this.string1 = string1;
            this.string2 = string2;
            this.args = args;
        }

        /// <summary>
        /// Represents the type of a <see cref="LocalisableStringDescriptor"/>.
        /// </summary>
        private enum StringLocalisationType
        {
            /// <summary>
            /// The <see cref="LocalisableStringDescriptor"/> is constructed from plain text and not localisable.
            /// </summary>
            None,

            /// <summary>
            /// The <see cref="LocalisableStringDescriptor"/> is constructed from Romanization/Unicode pair.
            /// </summary>
            Romanization,

            /// <summary>
            /// The <see cref="LocalisableStringDescriptor"/> is constructed from translation key.
            /// </summary>
            Translation,
        }
    }
}
