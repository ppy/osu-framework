// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        private IComparer<T> comparer;
        public event Action<T> OnRemoved;

        public LifetimeList(IComparer<T> comparer) : base(comparer)
        {
            this.comparer = comparer;
            AliveItems = new SortedList<T>(comparer);
        }

        public SortedList<T> AliveItems { get; }
        private List<bool> current = new List<bool>();

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
                    if (!current[i])
                    {
                        AliveItems.Add(obj);
                        current[i] = true;
                        anyAliveChanged = true;
                    }

                    if (!obj.IsLoaded)
                        obj.Load();
                }
                else
                {
                    if (current[i])
                    {
                        AliveItems.Remove(obj);
                        current[i] = false;
                        anyAliveChanged = true;
                    }

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

            int i = base.Add(item);
            current.Insert(i, false);

            return i;
        }

        public new bool Remove(T item)
        {
            int index = IndexOf(item);

            if (index < 0) return false;

            RemoveAt(index);

            AliveItems.Remove(item);

            return true;
        }

        public new void RemoveAt(int index)
        {
            current.RemoveAt(index);
            base.RemoveAt(index);
        }

        public new int RemoveAll(Predicate<T> match)
        {
            int count = 0;
            int i;

            while ((i = FindIndex(match)) >= 0)
            {
                if (current[i])
                    AliveItems.Remove(this[i]);

                RemoveAt(i);

                count++;
            }

            return count;
        }

        public new void Clear()
        {
            AliveItems.Clear();
            current.Clear();
            base.Clear();
        }
    }
}
