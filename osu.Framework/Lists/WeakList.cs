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
    public class WeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        private readonly List<InvalidatableWeakReference> list = new List<InvalidatableWeakReference>();

        public void Add(T obj) => list.Add(new InvalidatableWeakReference(obj));

        public void Add(WeakReference<T> weakReference) => list.Add(new InvalidatableWeakReference(weakReference));

        public void Remove(T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Reference == null)
                    continue;

                if (!list[i].Reference.TryGetTarget(out var obj) || obj != item)
                    continue;

                list[i] = default;
                break;
            }
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Reference != weakReference)
                    continue;

                list[i] = default;
                return true;
            }

            return false;
        }

        public bool Contains(T item)
        {
            foreach (var t in list)
            {
                if (t.Reference == null)
                    continue;

                if (t.Reference.TryGetTarget(out var obj) && obj == item)
                    return true;
            }

            return false;
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            foreach (var t in list)
            {
                if (t.Reference != null && t.Reference == weakReference)
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < list.Count; i++)
                list[i] = default;
        }

        public Enumerator GetEnumerator()
        {
            list.RemoveAll(item => item.Reference == null || !item.Reference.TryGetTarget(out _));

            return new Enumerator(list);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private List<InvalidatableWeakReference> list;

            private int currentIndex;
            private T currentObject;

            internal Enumerator(List<InvalidatableWeakReference> list)
            {
                this.list = list;

                currentIndex = -1; // The first MoveNext() should bring the iterator to 0
                currentObject = null;
            }

            public bool MoveNext()
            {
                while (++currentIndex < list.Count)
                {
                    if (list[currentIndex].Reference == null || !list[currentIndex].Reference.TryGetTarget(out currentObject))
                        continue;

                    return true;
                }

                return false;
            }

            public void Reset()
            {
                currentIndex = -1;
                currentObject = null;
            }

            public readonly T Current => currentObject;

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                list = null;
                currentObject = null;
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
