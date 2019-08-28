// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A <see cref="IWeakList{T}"/> which locks all operations.
    /// </summary>
    public class LockedWeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        private readonly WeakList<T> list = new WeakList<T>();

        public void Add(T item)
        {
            lock (list)
                list.Add(item);
        }

        public void Add(WeakReference<T> weakReference)
        {
            lock (list)
                list.Add(weakReference);
        }

        public void Remove(T item)
        {
            lock (list)
                list.Remove(item);
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            lock (list)
                return list.Remove(weakReference);
        }

        public bool Contains(T item)
        {
            lock (list)
                return list.Contains(item);
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            lock (list)
                return list.Contains(weakReference);
        }

        public void Clear()
        {
            lock (list)
                list.Clear();
        }

        [Obsolete("Use foreach() / GetEnumerator() (see: https://github.com/ppy/osu-framework/pull/2412)")] // can be removed 20191118
        public void ForEachAlive(Action<T> action)
        {
            foreach (var item in this)
                action(item);
        }

        public Enumerator GetEnumerator() => new Enumerator(list);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly WeakList<T> list;

            private WeakList<T>.Enumerator listEnumerator;

            private readonly bool lockTaken;

            internal Enumerator(WeakList<T> list)
            {
                this.list = list;

                listEnumerator = list.GetEnumerator();

                lockTaken = false;
                Monitor.Enter(list, ref lockTaken);
            }

            public bool MoveNext() => listEnumerator.MoveNext();

            public void Reset() => listEnumerator.Reset();

            public T Current => listEnumerator.Current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                if (lockTaken)
                    Monitor.Exit(list);
            }
        }
    }
}
