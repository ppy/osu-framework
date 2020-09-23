// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public class WeakList<T> : IWeakList<T>, IEnumerable<T>
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
            int hashCode = item?.GetHashCode() ?? 0;
            var enumerator = getEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (!enumerator.CheckEquals(item, hashCode))
                    continue;

                RemoveAt(enumerator.CurrentItemIndex);
                return true;
            }

            return false;
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            var enumerator = getEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (!enumerator.CheckEquals(weakReference))
                    continue;

                RemoveAt(enumerator.CurrentItemIndex);
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
            int hashCode = item?.GetHashCode() ?? 0;
            var enumerator = getEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.CheckEquals(item, hashCode))
                    return true;
            }

            return false;
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            var enumerator = getEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.CheckEquals(weakReference))
                    return true;
            }

            return false;
        }

        public void Clear() => listStart = listEnd = 0;

        public Enumerator GetEnumerator()
        {
            // Trim from the sides - items that have been removed.
            list.RemoveRange(listEnd, list.Count - listEnd);
            list.RemoveRange(0, listStart);

            // Trim all items whose references are no longer alive.
            list.RemoveAll(item => item.Reference == null || !item.Reference.TryGetTarget(out _));

            // After the trim, the valid range represents the full list.
            listStart = 0;
            listEnd = list.Count;

            return getEnumeratorNoTrim(true);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Enumerator getEnumeratorNoTrim(bool onlyValid = false) => new Enumerator(this, onlyValid);

        /// <summary>
        /// A <see cref="WeakList{T}"/> enumerator.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private WeakList<T> weakList;
            private readonly bool onlyValid;

            /// <summary>
            /// Creates a new <see cref="Enumerator"/>.
            /// </summary>
            /// <param name="weakList">The <see cref="WeakList{T}"/> to enumerate over.</param>
            /// <param name="onlyValid">Whether only the valid items in <paramref name="weakList"/> should be enumerated.
            /// If <c>false</c>, the user is expected to check the validity of <see cref="Current"/> prior to usage.</param>
            internal Enumerator(WeakList<T> weakList, bool onlyValid)
            {
                this.weakList = weakList;
                this.onlyValid = onlyValid;

                CurrentItemIndex = -1; // The first MoveNext() should bring the iterator to the start
                currentItem = default;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    ++CurrentItemIndex;

                    int index = weakList.listStart + CurrentItemIndex;

                    // Check whether we're still within the valid range of the list.
                    if (index >= weakList.listEnd)
                        return false;

                    var weakReference = weakList.list[index].Reference;

                    // Check whether the reference exists.
                    if (weakReference == null)
                    {
                        // If the reference doesn't exist, it must have previously been removed and can be skipped.
                        continue;
                    }

                    if (onlyValid && Current == null)
                    {
                        // If the object can't be retrieved, mark the reference for removal.
                        // The removal will occur on the _next_ enumeration (see: GetEnumerator()).
                        weakList.RemoveAt(CurrentItemIndex);
                        continue;
                    }

                    currentItem = weakList.list[index];
                    return true;
                }
            }

            public void Reset()
            {
                CurrentItemIndex = -1;
                currentItem = default;
            }

            public readonly T Current
            {
                get
                {
                    T current = null;
                    currentItem.Reference?.TryGetTarget(out current);
                    return current;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool CheckEquals(T obj, int hashCode)
                => currentItem.ObjectHashCode == hashCode && Current == obj;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool CheckEquals(WeakReference<T> weakReference)
                => currentItem.Reference == weakReference;

            private InvalidatableWeakReference currentItem;

            internal int CurrentItemIndex { get; private set; }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                weakList = null;
                currentItem = default;
            }
        }

        internal readonly struct InvalidatableWeakReference
        {
            [CanBeNull]
            public readonly WeakReference<T> Reference;

            /// <summary>
            /// Hash code of the target of <see cref="Reference"/>.
            /// </summary>
            public readonly int ObjectHashCode;

            public InvalidatableWeakReference([CanBeNull] T reference)
            {
                Reference = new WeakReference<T>(reference);
                ObjectHashCode = reference?.GetHashCode() ?? 0;
            }

            public InvalidatableWeakReference([CanBeNull] WeakReference<T> weakReference)
            {
                Reference = weakReference;

                if (Reference == null || !Reference.TryGetTarget(out var target))
                    ObjectHashCode = 0;
                else
                    ObjectHashCode = target.GetHashCode();
            }
        }
    }
}
