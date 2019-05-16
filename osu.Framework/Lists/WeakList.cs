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
        private readonly List<WeakReference<T>> list = new List<WeakReference<T>>();

        public void Add(T obj) => Add(new WeakReference<T>(obj));

        public void Add(WeakReference<T> weakReference) => list.Add(weakReference);

        public void Remove(T item) => list.RemoveAll(t => t.TryGetTarget(out var obj) && obj == item);

        public bool Remove(WeakReference<T> weakReference) => list.Remove(weakReference);

        public bool Contains(T item) => list.Any(t => t.TryGetTarget(out var obj) && obj == item);

        public bool Contains(WeakReference<T> weakReference) => list.Contains(weakReference);

        public void Clear() => list.Clear();

        public void ForEachAlive(Action<T> action)
        {
            foreach (var obj in this)
                action(obj);
        }

        public Enumerator GetEnumerator()
        {
            list.RemoveAll(item => !item.TryGetTarget(out _));

            return new Enumerator(list);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private List<WeakReference<T>> list;

            private int currentIndex;
            private T currentObject;

            internal Enumerator(List<WeakReference<T>> list)
            {
                this.list = list;

                currentIndex = -1; // The first MoveNext() should bring the iterator to 0
                currentObject = null;
            }

            public bool MoveNext()
            {
                while (++currentIndex < list.Count)
                {
                    if (!list[currentIndex].TryGetTarget(out currentObject))
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
    }
}
