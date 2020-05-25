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
            var enumerator = GetEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current != item)
                    continue;

                RemoveAt(enumerator.CurrentOffset);
                return true;
            }

            return false;
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            var enumerator = GetEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.CurrentReference != weakReference)
                    continue;

                RemoveAt(enumerator.CurrentOffset);
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
            var enumerator = GetEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current == item)
                    return true;
            }

            return false;
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            var enumerator = GetEnumeratorNoTrim();

            while (enumerator.MoveNext())
            {
                if (enumerator.CurrentReference == weakReference)
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

            return GetEnumeratorNoTrim();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator GetEnumeratorNoTrim() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private WeakList<T> weakList;
            private T currentObject;

            internal Enumerator(WeakList<T> weakList)
            {
                this.weakList = weakList;

                CurrentOffset = -1; // The first MoveNext() should bring the iterator to the start
                CurrentReference = null;
                currentObject = null;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    ++CurrentOffset;

                    int index = weakList.listStart + CurrentOffset;

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

                    // Check whether the object can be retrieved.
                    if (!weakReference.TryGetTarget(out currentObject))
                    {
                        // If the object can't be retrieved, mark the reference for removal.
                        // The removal will occur on the _next_ enumeration (see: GetEnumerator()).
                        weakList.RemoveAt(CurrentOffset);
                        continue;
                    }

                    CurrentReference = weakReference;
                    return true;
                }
            }

            public void Reset()
            {
                CurrentOffset = -1;
                CurrentReference = null;
                currentObject = null;
            }

            public readonly T Current => currentObject;

            internal WeakReference<T> CurrentReference { get; private set; }

            internal int CurrentOffset { get; private set; }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                weakList = null;
                currentObject = null;
                CurrentReference = null;
            }
        }

        internal readonly struct InvalidatableWeakReference
        {
            [CanBeNull]
            public readonly WeakReference<T> Reference;

            public InvalidatableWeakReference([CanBeNull] T reference)
                : this(new WeakReference<T>(reference))
            {
            }

            public InvalidatableWeakReference([CanBeNull] WeakReference<T> weakReference)
            {
                Reference = weakReference;
            }
        }
    }
}
