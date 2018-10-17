// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    /// <summary>
    /// A list maintaining weak reference of objects.
    /// </summary>
    /// <typeparam name="T">Type of items tracked by weak reference.</typeparam>
    public class WeakList<T> : List<WeakReference<T>>
        where T : class
    {
        public void Add(T obj)
        {
            moveFreeWeakReferences();

            var freeWeakReference = Find(weakRef => !weakRef.TryGetTarget(out _));

            if (freeWeakReference != null)
                freeWeakReference.SetTarget(obj);
            else
                Add(new WeakReference<T>(obj));
        }

        private void moveFreeWeakReferences()
        {
            for (int i = Count - 2; i >= 0; i--)
            {
                var item = this[i];
                var nextItem = this[i + 1];

                if (!item.TryGetTarget(out _) && nextItem.TryGetTarget(out _))
                {
                    RemoveAt(i);
                    Add(item);
                }
            }
        }

        /// <summary>
        /// Iterate on alive items, and remove non-alive references.
        /// </summary>
        public void ForEachAlive(Action<T> action)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].TryGetTarget(out T obj))
                    action(obj);
            }
        }
    }
}
