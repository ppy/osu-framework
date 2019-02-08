// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public class WeakList<T> : IWeakList<T>
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
            int index = 0;
            while (index < list.Count)
            {
                if (list[index].TryGetTarget(out T obj))
                {
                    action(obj);
                    index++;
                }
                else
                    list.RemoveAt(index);
            }
        }
    }
}
