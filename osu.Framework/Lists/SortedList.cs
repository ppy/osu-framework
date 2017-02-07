// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace osu.Framework.Lists
{
    public class SortedList<T> : List<T>
    {
        public IComparer<T> Comparer { get; private set; }

        public SortedList(IComparer<T> comparer)
        {
            Comparer = comparer;
        }

        public new int Add(T value)
        {
            Debug.Assert(value != null);
            Debug.Assert(value is T);

            int index = getAdditionIndex(value);
            Insert(index, value);

            return index;
        }

        public new int IndexOf(T value)
        {
            return BinarySearch(value, Comparer);
        }

        /// <summary>
        /// Gets the first index of the element larger than value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The index of the first element larger than value.</returns>
        private int getAdditionIndex(T value)
        {
            int index = BinarySearch(value, Comparer);
            if (index < 0)
                index = ~index;

            // Binary search is not guaranteed to give the last index 
            // when duplicates are involved, so let's move towards it
            for (; index < Count; index++)
            {
                if (Comparer.Compare(this[index], value) != 0)
                    break;
            }

            return index;
        }
    }
}
