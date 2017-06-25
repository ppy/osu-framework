// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Lists
{
    public class SortedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        private readonly List<T> list;

        public IComparer<T> Comparer { get; }

        public int Count => list.Count;

        bool ICollection<T>.IsReadOnly => ((ICollection<T>)list).IsReadOnly;
        
        public T this[int index] => list[index];

        public SortedList(Func<T,T,int> comparer)
            : this(new ComparisonComparer<T>(comparer))
        {
        }

        public SortedList(IComparer<T> comparer)
        {
            list = new List<T>();
            Comparer = comparer;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var i in collection) Add(i);
        }

        public virtual int Add(T value) => addInternal(value);

        /// <summary>
        /// Adds the specified item internally without the interference of a possible derived class.
        /// </summary>
        /// <param name="value">The item to add.</param>
        /// <returns>The index of the item within this list.</returns>
        private int addInternal(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int index = list.BinarySearch(value, Comparer);

            if (index < 0)
                index = ~index;

            list.Insert(index, value);

            return index;
        }

        public virtual bool Remove(T item) => list.Remove(item);

        public void RemoveAt(int index) => Remove(this[index]);

        public int RemoveAll(Predicate<T> match)
        {
            List<T> found = (List<T>)FindAll(match);

            foreach (var i in found)
                Remove(i);

            return found.Count;
        }

        public void Clear()
        {
            foreach (var i in list.ToList())
                Remove(i);
        }

        public bool Contains(T item) => list.Contains(item);

        public int IndexOf(T value) => list.IndexOf(value);

        /// <summary>
        /// Repositions the item within this list using <see cref="Comparer"/>.
        /// Useful when the primary sorting property of the item had changed.
        /// </summary>
        /// <param name="item">The item to update its actual index.</param>
        public virtual void UpdateSorting(T item)
        {
            list.Remove(item);

            // Add it back in the right place
            // Must internally add because it was internally removed.
            // Derived classes which keeps other sorted list (such as LifetimeList) will extend this function to keep its own lists sorted.
            addInternal(item);
        }

        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public T Find(Predicate<T> match) => list.Find(match);

        public IEnumerable<T> FindAll(Predicate<T> match) => list.FindAll(match);

        public T FindLast(Predicate<T> match) => list.FindLast(match);

        public int FindIndex(Predicate<T> match) => list.FindIndex(match);

        public override string ToString() => $@"{GetType().ReadableName()} ({Count} items)";

        #region ICollection<T> Implementation

        void ICollection<T>.Add(T item) => Add(item);

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        #endregion

        private class ComparisonComparer<TComparison> : IComparer<TComparison>
        {
            private readonly Comparison<TComparison> comparison;

            public ComparisonComparer(Func<TComparison, TComparison, int> compare)
            {
                if (compare == null)
                {
                    throw new ArgumentNullException(nameof(compare));
                }
                comparison = new Comparison<TComparison>(compare);
            }

            public int Compare(TComparison x, TComparison y)
            {
                return comparison(x, y);
            }
        }
    }
}
