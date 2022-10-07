// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;

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

        /// <summary>
        /// Replaces one or more format items in a specified string with a localised string representation of a corresponding object in <paramref name="args"/>.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The objects to format.</param>
        public static LocalisableString Format(string format, params object?[] args) => new LocalisableFormattableString(format, args);

        /// <summary>
        /// Creates a <see cref="LocalisableString"/> representation of the specified interpolated string.
        /// </summary>
        /// <param name="interpolation">The interpolated string containing format and arguments.</param>
        public static LocalisableString Interpolate(FormattableString interpolation) => new LocalisableFormattableString(interpolation);

        /// <summary>
        /// Indicates whether the specified <see cref="LocalisableString"/> is <see langword="null"/> or an empty <see langword="string"/>.
        /// </summary>
        /// <remarks>
        /// Will always be false for non-<see langword="string"/> data (e.g. <see cref="TranslatableString"/>).
        /// </remarks>
        /// <param name="value">The <see cref="LocalisableString"/> to test.</param>
        /// <returns><see langword="true"/> if the <paramref name="value"/> parameter is <see langword="null"/> or an empty <see langword="string"/> (&quot;&quot;);
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsNullOrEmpty([NotNullWhen(false)] LocalisableString? value)
        {
            if (value is not LocalisableString localisable)
                return true;

            if (localisable.Data == null)
                return true;

            if (localisable.Data is not string strData)
                return false;

            return string.IsNullOrEmpty(strData);
        }

        /// <summary>
        /// Indicates whether the specified <see cref="LocalisableString"/> is <see langword="null"/>, an empty <see langword="string"/>,
        /// or a <see langword="string"/> that consists of only white-space characters.
        /// </summary>
        /// <remarks>
        /// Will always be false for non-<see langword="string"/> data (e.g. <see cref="TranslatableString"/>).
        /// </remarks>
        /// <param name="value">The <see cref="LocalisableString"/> to test.</param>
        /// <returns><see langword="true"/> if the <paramref name="value"/> parameter is <see langword="null"/>, an empty <see langword="string"/> (&quot;&quot;),
        /// or a <see langword="string"/> that consists of only white-space characters.</returns>
        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] LocalisableString? value)
        {
            if (value is not LocalisableString localisable)
                return true;

            if (localisable.Data == null)
                return true;

            if (localisable.Data is not string strData)
                return false;

            return string.IsNullOrWhiteSpace(strData);
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
