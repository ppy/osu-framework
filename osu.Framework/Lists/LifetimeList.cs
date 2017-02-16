// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Timing;

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
        private readonly List<bool> current = new List<bool>();

        /// <summary>
        /// Updates the life status of this LifetimeList's children.
        /// </summary>
        /// <returns>Whether any alive states were changed.</returns>
        public virtual bool Update(FrameTimeInfo time)
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < Count; i++)
            {
                var item = this[i];
                item.UpdateTime(time);
                anyAliveChanged |= CheckItem(item, ref i);
            }

            return anyAliveChanged;
        }

        protected bool CheckItem(T item, ref int i)
        {
            bool changed = false;

            if (item.IsAlive)
            {
                if (!current[i])
                {
                    LoadRequested?.Invoke(item);
                    if (item.IsLoaded)
                    {
                        AliveItems.Add(item);
                        current[i] = true;
                        changed = true;
                    }
                }
            }
            else
            {
                if (current[i])
                {
                    AliveItems.Remove(item);
                    current[i] = false;
                    changed = true;
                }

                if (item.RemoveWhenNotAlive)
                {
                    Removed?.Invoke(item);
                    RemoveAt(i--);
                }
            }

            return changed;
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
