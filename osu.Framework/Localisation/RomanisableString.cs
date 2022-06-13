// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string that has a romanised fallback to allow a better experience for users that potentially can't read the original script.
    /// See <see cref="FrameworkSetting.ShowUnicode"/>, which can toggle the display of romanised variants.
    /// </summary>
    public class RomanisableString : IEquatable<RomanisableString>, ILocalisableStringData
    {
        /// <summary>
        /// The string in its original script. May be null.
        /// </summary>
        public readonly string? Original;

        /// <summary>
        /// The romanised version of the string. May be null.
        /// </summary>
        public readonly string? Romanised;

        /// <summary>
        /// Construct a new romanisable string.
        /// </summary>
        /// <remarks>
        /// For flexibility, both of the provided strings are allowed to be null. If both are null, the returned string value from <see cref="GetLocalised"/> will be <see cref="string.Empty"/>.
        /// </remarks>
        /// <param name="original">The string in its original script. If null, the <paramref name="romanised"/> version will always be used.</param>
        /// <param name="romanised">The romanised version of the string. If null, the <paramref name="original"/> version will always be used.</param>
        public RomanisableString(string? original, string? romanised)
        {
            Original = original;
            Romanised = romanised;
        }

        public string GetLocalised(LocalisationParameters parameters) => GetPreferred(parameters.PreferOriginalScript);

        /// <summary>
        /// Get the best match for this string based on a user preference for which should be displayed.
        /// </summary>
        /// <param name="preferUnicode">Whether to prefer the unicode (aka original) version where available.</param>
        /// <returns>The best match for the provided criteria.</returns>
        public string GetPreferred(bool preferUnicode)
        {
            if (string.IsNullOrEmpty(Romanised)) return Original ?? string.Empty;
            if (string.IsNullOrEmpty(Original)) return Romanised ?? string.Empty;

            return preferUnicode ? Original : Romanised;
        }

        public override string ToString() => GetLocalised(new LocalisationParameters(null, false));

        public bool Equals(RomanisableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Original == other.Original
                   && Romanised == other.Romanised;
        }

        public bool Equals(ILocalisableStringData? other) => other is RomanisableString romanisable && Equals(romanisable);
        public override bool Equals(object? obj) => obj is RomanisableString romanisable && Equals(romanisable);

        public override int GetHashCode()
        {
            return HashCode.Combine(Original, Romanised);
        }
    }
}
