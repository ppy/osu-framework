// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        public event Action<T> Removed;

        public event Action<T> LoadRequested;

        public LifetimeList(IComparer<T> comparer) : base(comparer)
        {
            AliveItems = new SortedList<T>(comparer);
        }

        public SortedList<T> AliveItems { get; }
        private List<bool> current = new List<bool>();

        /// <summary>
        /// Updates the life status of this LifetimeList's children.
        /// </summary>
        /// <returns>Whether any alive states were changed.</returns>
        public bool Update(double time)
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < Count; i++)
            {
                var item = this[i];

                item.UpdateTime(time);

                if (item.IsAlive)
                {
                    if (!current[i])
                    {
                        LoadRequested?.Invoke(item);
                        if (item.IsLoaded)
                        {
                            AliveItems.Add(item);
                            current[i] = true;
                            anyAliveChanged = true;
                        }
                    }
                }
                else
                {
                    if (current[i])
                    {
                        AliveItems.Remove(item);
                        current[i] = false;
                        anyAliveChanged = true;
                    }

                    if (item.RemoveWhenNotAlive)
                    {
                        RemoveAt(i--);
                        Removed?.Invoke(item);
                    }
                }
            }

            return anyAliveChanged;
        }

        public new int Add(T item)
        {
            if (item.IsAlive && !item.IsLoaded)
                LoadRequested?.Invoke(item);

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
