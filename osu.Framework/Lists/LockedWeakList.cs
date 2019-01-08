// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A <see cref="IWeakList{T}"/> which locks all operations.
    /// </summary>
    public class LockedWeakList<T> : IWeakList<T>
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

        public void ForEachAlive(Action<T> action)
        {
            lock (list)
                list.ForEachAlive(action);
        }
    }
}
