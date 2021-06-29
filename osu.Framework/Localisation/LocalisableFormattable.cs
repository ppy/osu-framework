// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string allowing for formatting <see cref="IFormattable"/>s using the current locale.
    /// </summary>
    public class LocalisableFormattable : IEquatable<LocalisableFormattable>
    {
        public readonly IFormattable Value;
        public readonly string Format;

        /// <summary>
        /// Creates a <see cref="LocalisableFormattable"/> with an <see cref="IFormattable"/> value and a format string.
        /// </summary>
        /// <param name="value">The <see cref="IFormattable"/> value.</param>
        /// <param name="format">The format string.</param>
        public LocalisableFormattable(IFormattable value, string format)
        {
            Value = value;
            Format = format;
        }

        public string ToString(IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                return ToString();

            return Value.ToString(Format, formatProvider);
        }

        public override string ToString() => Value.ToString(Format, CultureInfo.InvariantCulture);

        public bool Equals(LocalisableFormattable other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return EqualityComparer<object>.Default.Equals(Value, other.Value) &&
                   Format == other.Format;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((LocalisableFormattable)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Value, Format);
    }
}
