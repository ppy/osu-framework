// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace osu.Framework.Data
{
    public class PropertyGroupComparer<T> : IComparer<T>
    {
        public ObservableCollection<PropertyGroupDescription> GroupDescriptions { get; set; } = new ObservableCollection<PropertyGroupDescription>();

        public virtual int Compare(T x, T y)
        {
            return Compare(x, y, GroupDescriptions);
        }

        public virtual int Compare(T x, T y, ObservableCollection<PropertyGroupDescription> descriptions)
        {
            foreach (PropertyGroupDescription description in descriptions)
            {
                int comparison = CompareProperty(x, y, description);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        }

        public virtual int CompareProperty(T x, T y, PropertyGroupDescription description)
        {
            object xp = x.GetType().GetProperty(description.PropertyName).GetValue(x);
            object yp = y.GetType().GetProperty(description.PropertyName).GetValue(y);

            int comparison;

            if (description.CustomSort != null)
                comparison = description.CustomSort.Compare(xp, yp);
            else
                comparison = (xp is IComparable) ?
                    (xp as IComparable).CompareTo(yp) : String.CompareOrdinal(xp.ToString(), yp.ToString());

            return description.CompareDescending ? -comparison : comparison;
        }
    }
}
