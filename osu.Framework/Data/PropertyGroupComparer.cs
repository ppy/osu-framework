// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace osu.Framework.Data
{
    public class PropertyComparer : IComparer<object>
    {
        public ObservableCollection<PropertyDescription> Descriptions { get; set; }
            = new ObservableCollection<PropertyDescription>();

        /// <summary>
        /// Compares two objects using the collection of Property Descriptions provided in this class.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <returns>
        /// Positive integer if the second object is greater than the first one based on the
        /// property descriptions defined;
        /// Negative integer if the first object is gretaer than the second one;
        /// 0 if both objects have the same value on the defined properties.
        /// </returns>
        public virtual int Compare(object x, object y)
        {
            return Compare(x, y, Descriptions);
        }

        /// <summary>
        /// Compares two objects using an specific collection of Property Descriptions.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <param name="descriptions">Collection of property descriptions to compare by.</param>
        /// <returns>
        /// Positive integer if the second object is greater than the first one based on the
        /// property descriptions defined;
        /// Negative integer if the first object is gretaer than the second one;
        /// 0 if both objects have the same value on the defined properties.
        /// </returns>
        public virtual int Compare(
            object x, object y, ObservableCollection<PropertyDescription> descriptions)
        {
            foreach (PropertyDescription description in descriptions)
            {
                int comparison = CompareProperty(x, y, description);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        }

        /// <summary>
        /// Compare values of a defined property of two objects.
        /// </summary>
        /// <param name="x">First object.</param>
        /// <param name="y">Second object.</param>
        /// <param name="description">Property description to compare by.</param>
        /// <returns>
        /// Positive integer if the second object property value is greater than the first one value;
        /// Positive integer if the first object property value is greater than the second one;
        /// 0 if both objects have the same value on the defined property.
        /// </returns>
        public virtual int CompareProperty(object x, object y, PropertyDescription description)
        {
            object xp = description.GetPropertyValue(x);
            object yp = description.GetPropertyValue(y);

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
