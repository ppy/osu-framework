// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A descriptor representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisableString : IEquatable<LocalisableString>
    {
        /// <summary>
        /// The underlying data, can be <see cref="string"/>, <see cref="TranslatableString"/>, or <see cref="RomanisableString"/>.
        /// </summary>
        internal readonly object? Data;

        private LocalisableString(object data) => Data = data;

        // it's somehow common to call default(LocalisableString), and we should return empty string then.
        public override string ToString() => Data?.ToString() ?? string.Empty;

        public bool Equals(LocalisableString other)
        {
            bool thisIsNull = ReferenceEquals(null, Data);
            bool otherIsNull = ReferenceEquals(null, other.Data);

            // Nullability differs.
            if (thisIsNull != otherIsNull)
                return false;

            // Both are null.
            if (thisIsNull)
            {
                Debug.Assert(otherIsNull);
                return true;
            }

            if (Data is string strThis)
            {
                if (other.Data is string strOther)
                    return strThis.Equals(strOther, StringComparison.Ordinal);

                return false;
            }

            if (Data is TranslatableString translatableThis)
            {
                if (other.Data is TranslatableString translatableOther)
                    return translatableThis.Equals(translatableOther);

                return false;
            }

            if (Data is RomanisableString romanisableThis)
            {
                if (other.Data is RomanisableString romanisableOther)
                    return romanisableThis.Equals(romanisableOther);
            }

            Debug.Assert(Data != null);
            Debug.Assert(other.Data != null);
            return EqualityComparer<object>.Default.Equals(Data, other.Data);
        }

        public override bool Equals(object? obj) => obj is LocalisableString other && Equals(other);

        public override int GetHashCode()
        {
            if (Data is string str)
                return str.GetHashCode();

            if (Data is TranslatableString translatable)
                return translatable.GetHashCode();

            if (Data is RomanisableString romanisable)
                return romanisable.GetHashCode();

            return Data?.GetHashCode() ?? 0;
        }

        public static implicit operator LocalisableString(string text) => new LocalisableString(text);
        public static implicit operator LocalisableString(TranslatableString translatable) => new LocalisableString(translatable);
        public static implicit operator LocalisableString(RomanisableString romanisable) => new LocalisableString(romanisable);
    }
}
