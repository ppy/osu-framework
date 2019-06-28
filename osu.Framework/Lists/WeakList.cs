// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public class WeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        private readonly List<InvalidatableWeakReference<T>> list = new List<InvalidatableWeakReference<T>>();

        public void Add(T obj) => list.Add(new InvalidatableWeakReference<T>(obj));

        public void Add(WeakReference<T> weakReference) => list.Add(new InvalidatableWeakReference<T>(weakReference));

        public void Remove(T item)
        {
            foreach (var i in list)
            {
                if (i.Reference.TryGetTarget(out var obj) && obj == item)
                {
                    i.Invalidate();
                    return;
                }
            }
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            bool found = false;

            foreach (var item in list)
            {
                if (item.Reference == weakReference)
                {
                    item.Invalidate();
                    found = true;
                }
            }

            return found;
        }

        public bool Contains(T item) => list.Any(t => t.Reference.TryGetTarget(out var obj) && obj == item);

        public bool Contains(WeakReference<T> weakReference) => list.Any(t => t.Reference == weakReference);

        public void Clear()
        {
            foreach (var item in list)
                item.Invalidate();
        }

        [Obsolete("Use foreach() / GetEnumerator() (see: https://github.com/ppy/osu-framework/pull/2412)")]
        public void ForEachAlive(Action<T> action)
        {
            foreach (var obj in this)
                action(obj);
        }

        public Enumerator GetEnumerator()
        {
            list.RemoveAll(item => item.Invalid || !item.Reference.TryGetTarget(out _));

            return new Enumerator(list);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private List<InvalidatableWeakReference<T>> list;

            private int currentIndex;
            private T currentObject;

            internal Enumerator(List<InvalidatableWeakReference<T>> list)
            {
                this.list = list;

                currentIndex = -1; // The first MoveNext() should bring the iterator to 0
                currentObject = null;
            }

            public bool MoveNext()
            {
                while (++currentIndex < list.Count)
                {
                    if (list[currentIndex].Invalid || !list[currentIndex].Reference.TryGetTarget(out currentObject))
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

            public T Current => currentObject;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                list = null;
                currentObject = null;
            }
        }

        internal class InvalidatableWeakReference<U>
            where U : class
        {
            public readonly WeakReference<U> Reference;

            public bool Invalid { get; private set; }

            public InvalidatableWeakReference(U reference)
                : this(new WeakReference<U>(reference))
            {
            }

            public InvalidatableWeakReference(WeakReference<U> weakReference)
            {
                Reference = weakReference;
            }

            public void Invalidate() => Invalid = true;
        }
    }
}
