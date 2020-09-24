// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public partial class WeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        private readonly List<InvalidatableWeakReference> list = new List<InvalidatableWeakReference>();
        private int listStart; // The inclusive starting index in the list.
        private int listEnd; // The exclusive ending index in the list.

        public void Add(T obj) => add(new InvalidatableWeakReference(obj));

        public void Add(WeakReference<T> weakReference) => add(new InvalidatableWeakReference(weakReference));

        private void add(in InvalidatableWeakReference item)
        {
            if (listEnd < list.Count)
                list[listEnd] = item;
            else
                list.Add(item);

            listEnd++;
        }

        public bool Remove(T item)
        {
            int hashCode = item == null ? 0 : EqualityComparer<T>.Default.GetHashCode(item);
            var enumerator = new AllItemsEnumerator(this);

            while (enumerator.MoveNext())
            {
                if (!enumerator.CheckEquals(item, hashCode))
                    continue;

                RemoveAt(enumerator.CurrentItemIndex - listStart);
                return true;
            }

            return false;
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            var enumerator = new AllItemsEnumerator(this);

            while (enumerator.MoveNext())
            {
                if (!enumerator.CheckEquals(weakReference))
                    continue;

                RemoveAt(enumerator.CurrentItemIndex - listStart);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            // Move the index to the valid range of the list.
            index += listStart;

            if (index < listStart || index >= listEnd)
                throw new ArgumentOutOfRangeException(nameof(index));

            list[index] = default;

            if (index == listStart)
                listStart++;
            else if (index == listEnd - 1)
                listEnd--;
        }

        public bool Contains(T item)
        {
            int hashCode = item == null ? 0 : EqualityComparer<T>.Default.GetHashCode(item);
            var enumerator = new AllItemsEnumerator(this);

            while (enumerator.MoveNext())
            {
                if (enumerator.CheckEquals(item, hashCode))
                    return true;
            }

            return false;
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            var enumerator = new AllItemsEnumerator(this);

            while (enumerator.MoveNext())
            {
                if (enumerator.CheckEquals(weakReference))
                    return true;
            }

            return false;
        }

        public void Clear() => listStart = listEnd = 0;

        public ValidItemsEnumerator GetEnumerator()
        {
            // Trim from the sides - items that have been removed.
            list.RemoveRange(listEnd, list.Count - listEnd);
            list.RemoveRange(0, listStart);

            // Trim all items whose references are no longer alive.
            list.RemoveAll(item => item.Reference == null || !item.Reference.TryGetTarget(out _));

            // After the trim, the valid range represents the full list.
            listStart = 0;
            listEnd = list.Count;

            return new ValidItemsEnumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal readonly struct InvalidatableWeakReference
        {
            [CanBeNull]
            public readonly WeakReference<T> Reference;

            /// <summary>
            /// Hash code of the target of <see cref="Reference"/>.
            /// </summary>
            public readonly int ObjectHashCode;

            public InvalidatableWeakReference(T reference)
            {
                Reference = new WeakReference<T>(reference);
                ObjectHashCode = reference == null ? 0 : EqualityComparer<T>.Default.GetHashCode(reference);
            }

            public InvalidatableWeakReference(WeakReference<T> weakReference)
            {
                Reference = weakReference;

                if (Reference == null || !Reference.TryGetTarget(out var target))
                    ObjectHashCode = 0;
                else
                    ObjectHashCode = EqualityComparer<T>.Default.GetHashCode(target);
            }
        }
    }
}
