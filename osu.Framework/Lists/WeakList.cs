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
        private readonly List<InvalidatableWeakReference> list = new List<InvalidatableWeakReference>();

        public void Add(T obj) => list.Add(new InvalidatableWeakReference(obj));

        public void Add(WeakReference<T> weakReference) => list.Add(new InvalidatableWeakReference(weakReference));

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

        [Obsolete("Use foreach() / GetEnumerator() (see: https://github.com/ppy/osu-framework/pull/2412)")] // can be removed 20191118
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

        internal class InvalidatableWeakReference
        {
            public readonly WeakReference<T> Reference;

            public bool Invalid { get; private set; }

            public InvalidatableWeakReference(T reference)
                : this(new WeakReference<T>(reference))
            {
            }

            public InvalidatableWeakReference(WeakReference<T> weakReference)
            {
                Reference = weakReference;
            }

            public void Invalidate() => Invalid = true;
        }
    }
}
