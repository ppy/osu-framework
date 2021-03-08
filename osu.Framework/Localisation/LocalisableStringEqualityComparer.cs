// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            bool xIsNull = ReferenceEquals(null, x.Data);
            bool yIsNull = ReferenceEquals(null, y.Data);

            // Nullability differs.
            if (xIsNull != yIsNull)
                return false;

            // Both are null.
            if (xIsNull)
            {
                Debug.Assert(yIsNull);
                return true;
            }

            if (x.Data is string strX)
            {
                if (y.Data is string strY)
                    return strX.Equals(strY, StringComparison.Ordinal);

                return false;
            }

            if (x.Data is TranslatableString translatableX)
            {
                if (y.Data is TranslatableString translatableY)
                    return translatableX.Equals(translatableY);

                return false;
            }

            if (x.Data is RomanisableString romanisableX)
            {
                if (y.Data is RomanisableString romanisableY)
                    return romanisableX.Equals(romanisableY);

                return false;
            }

            Debug.Assert(x.Data != null);
            Debug.Assert(y.Data != null);
            return EqualityComparer<object>.Default.Equals(x.Data, y.Data);
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
