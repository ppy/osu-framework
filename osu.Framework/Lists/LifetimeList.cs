// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        public event Action<T> OnRemoved;

        public LifetimeList(IComparer<T> comparer) : base(comparer)
        {
            Current = new List<T>();
        }

        public List<T> Current { get; }

        /// <summary>
        /// Updates the life status of this LifetimeList's children.
        /// </summary>
        /// <returns>Whether any alive states were changed.</returns>
        public bool Update()
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < Count; i++)
            {
                var obj = this[i];

                if (obj.IsAlive)
                {
                    if (!Current.Contains(obj))
                    {
                        Current.Add(obj);
                        anyAliveChanged = true;
                    }

                    if (!obj.IsLoaded)
                        obj.Load();
                }
                else
                {
                    if (Current.Remove(obj))
                        anyAliveChanged = true;

                    if (obj.RemoveWhenNotAlive)
                    {
                        RemoveAt(i--);
                        OnRemoved?.Invoke(obj);
                    }
                }
            }
            
            return anyAliveChanged;
        }

        public new int Add(T item)
        {
            if (item.IsAlive && !item.IsLoaded)
                item.Load();
            
            return base.Add(item);
        }

        public new bool Remove(T item)
        {
            Current.Remove(item);
            return base.Remove(item);
        }

        public new int RemoveAll(Predicate<T> match)
        {
            Current.RemoveAll(match);
            return base.RemoveAll(match);
        }

        public new void Clear()
        {
            Current.Clear();
            base.Clear();
        }
    }
}
