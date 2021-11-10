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
        /// Creates a new <see cref="LocalisableString"/> with underlying string data.
        /// </summary>
        public LocalisableString(string data)
        {
            Data = data;
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/> with underlying localisable string data.
        /// </summary>
        public LocalisableString(ILocalisableStringData data)
        {
            Data = data;
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
        public static implicit operator LocalisableString(CaseTransformableString transformable) => new LocalisableString(transformable);

        public static bool operator ==(LocalisableString left, LocalisableString right) => left.Equals(right);
        public static bool operator !=(LocalisableString left, LocalisableString right) => !left.Equals(right);
    }
}
