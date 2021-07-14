// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string which can apply case transformations to underlying localisable string data.
    /// </summary>
    public class TransformableString : IEquatable<TransformableString>, ILocalisableStringData
    {
        /// <summary>
        /// The case to apply to the underlying data.
        /// </summary>
        public readonly Casing Casing;

        /// <summary>
        /// The underlying localisable string data of this transformable string.
        /// </summary>
        public readonly ILocalisableStringData? Data;

        /// <summary>
        /// Constructs a new transformable string with specified underlying string data and casing.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="casing"></param>
        public TransformableString(ILocalisableStringData data, Casing casing)
        {
            Data = data;
            Casing = casing;
        }

        public string GetLocalised(LocalisationParameters parameters)
        {
            var cultureText = parameters.Store?.EffectiveCulture?.TextInfo ?? CultureInfo.CurrentCulture.TextInfo;
            var stringData = Data?.GetLocalised(parameters) ?? string.Empty;

            switch (Casing)
            {
                case Casing.UpperCase:
                    return cultureText.ToUpper(stringData);

                case Casing.TitleCase:
                    return cultureText.ToTitleCase(stringData);

                case Casing.LowerCase:
                    return cultureText.ToLower(stringData);

                case Casing.Default:
                default:
                    return stringData;
            }
        }

        public bool Equals(ILocalisableStringData? other) => other is TransformableString transformable && Equals(transformable);

        public bool Equals(TransformableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Casing == other.Casing && Data == other.Data;
        }
    }

    /// <summary>
    /// Case applicable to the underlying localisable string data of a <see cref="TransformableString"/>.
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
