// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A descriptor representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisableString : IEquatable<LocalisableString>
    {
        /// <summary>
        /// The underlying data.
        /// </summary>
        internal readonly object? Data;

        /// <summary>
        /// The case override to apply to the underlying string data.
        /// </summary>
        public readonly Casing Casing;

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/> with underlying string data.
        /// </summary>
        public LocalisableString(string data)
        {
            Data = data;
            Casing = Casing.Default;
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/> with underlying localisable string data.
        /// </summary>
        public LocalisableString(ILocalisableStringData data, Casing casing = Casing.Default)
        {
            Data = data;
            Casing = casing;
        }

        // it's somehow common to call default(LocalisableString), and we should return empty string then.
        public override string ToString() => Data?.ToString() ?? string.Empty;

        public bool Equals(LocalisableString other) => LocalisableStringEqualityComparer.Default.Equals(this, other);
        public override bool Equals(object? obj) => obj is LocalisableString other && Equals(other);
        public override int GetHashCode() => LocalisableStringEqualityComparer.Default.GetHashCode(this);

        public static implicit operator LocalisableString(string text) => new LocalisableString(text);
        public static implicit operator LocalisableString(TranslatableString translatable) => new LocalisableString(translatable);
        public static implicit operator LocalisableString(RomanisableString romanisable) => new LocalisableString(romanisable);
        public static implicit operator LocalisableString(LocalisableFormattableString formattable) => new LocalisableString(formattable);

        public static bool operator ==(LocalisableString left, LocalisableString right) => left.Equals(right);
        public static bool operator !=(LocalisableString left, LocalisableString right) => !left.Equals(right);
    }

    /// <summary>
    /// Case overrides applicable to the underlying string data of a <see cref="LocalisableString"/>.
    /// </summary>
    public enum Casing
    {
        /// <summary>
        /// Use the string data case.
        /// </summary>
        Default,

        /// <summary>
        /// Transform the string data to uppercase.
        /// </summary>
        Uppercase,

        /// <summary>
        /// Transform the string data to title case aka capitalized case
        /// </summary>
        TitleCase,

        /// <summary>
        /// Transform the string data to lowercase.
        /// </summary>
        Lowercase
    }
}
