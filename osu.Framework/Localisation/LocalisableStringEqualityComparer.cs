// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// An equality comparer for the <see cref="LocalisableString"/> type.
    /// </summary>
    public class LocalisableStringEqualityComparer : IEqualityComparer<LocalisableString>
    {
        // ReSharper disable once InconsistentNaming (follows EqualityComparer<T>.Default)
        public static readonly LocalisableStringEqualityComparer Default = new LocalisableStringEqualityComparer();

        public bool Equals(LocalisableString x, LocalisableString y)
        {
            var xData = x.Data;
            var yData = y.Data;

            if (ReferenceEquals(null, xData) != ReferenceEquals(null, yData))
                return false;

            if (ReferenceEquals(null, xData))
                return true;

            if (xData.GetType() != yData.GetType())
                return EqualityComparer<object>.Default.Equals(xData, yData);

            switch (xData)
            {
                case string strX:
                    return strX.Equals((string)yData, StringComparison.Ordinal);

                case TranslatableString translatableX:
                    return translatableX.Equals((TranslatableString)yData);

                case RomanisableString romanisableX:
                    return romanisableX.Equals((RomanisableString)yData);
            }

            return false;
        }

        public int GetHashCode(LocalisableString obj)
        {
            if (ReferenceEquals(null, obj.Data))
                return 0;

            var hashCode = new HashCode();
            hashCode.Add(obj.Data.GetType().GetHashCode());

            switch (obj.Data)
            {
                case string str:
                    hashCode.Add(str.GetHashCode());
                    break;

                case TranslatableString translatable:
                    hashCode.Add(translatable.GetHashCode());
                    break;

                case RomanisableString romanisable:
                    hashCode.Add(romanisable.GetHashCode());
                    break;
            }

            return hashCode.ToHashCode();
        }
    }
}
