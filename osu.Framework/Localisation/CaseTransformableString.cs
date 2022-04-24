// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Globalization;
using Humanizer;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string which can apply case transformations to underlying localisable string.
    /// </summary>
    public class CaseTransformableString : IEquatable<CaseTransformableString>, ILocalisableStringData
    {
        /// <summary>
        /// The case to apply to the underlying data.
        /// </summary>
        public readonly Casing Casing;

        /// <summary>
        /// The underlying localisable string of this transformable string.
        /// </summary>
        public readonly LocalisableString String;

        /// <summary>
        /// Constructs a new transformable string with specified underlying localisable string and casing.
        /// </summary>
        /// <param name="str">The localisable string to apply case transformations on.</param>
        /// <param name="casing">The casing to use on the localisable string.</param>
        public CaseTransformableString(LocalisableString str, Casing casing)
        {
            String = str;
            Casing = casing;
        }

        public string GetLocalised(LocalisationParameters parameters)
        {
            string stringData = getStringData(parameters);
            var cultureInfo = parameters.Store?.EffectiveCulture ?? CultureInfo.InvariantCulture;

            switch (Casing)
            {
                case Casing.UpperCase:
                    return stringData.Transform(cultureInfo, To.UpperCase);

                case Casing.TitleCase:
                    return stringData.Transform(cultureInfo, To.TitleCase);

                case Casing.LowerCase:
                    return stringData.Transform(cultureInfo, To.LowerCase);

                case Casing.Default:
                default:
                    return stringData;
            }
        }

        public override string ToString() => GetLocalised(new LocalisationParameters(null, false));

        public bool Equals(ILocalisableStringData? other) => other is CaseTransformableString transformable && Equals(transformable);

        public bool Equals(CaseTransformableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Casing == other.Casing && String.Equals(other.String);
        }

        private string getStringData(LocalisationParameters localisationParameters)
        {
            switch (String.Data)
            {
                case string plain:
                    return plain;

                case ILocalisableStringData data:
                    return data.GetLocalised(localisationParameters);

                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Case applicable to the underlying localisable string of a <see cref="CaseTransformableString"/>.
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
        UpperCase,

        /// <summary>
        /// Transform the string data to title case aka capitalized case
        /// </summary>
        TitleCase,

        /// <summary>
        /// Transform the string data to lowercase.
        /// </summary>
        LowerCase
    }
}
