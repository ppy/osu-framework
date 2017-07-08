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

        public LifetimeList(IComparer<T> comparer)
            : base(comparer)
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

        public override int Add(T item)
        {
            LoadRequested?.Invoke(item);

            int i = base.Add(item);

            bool isLoaded = item.IsLoaded;
            current.Insert(i, isLoaded);
            if (isLoaded)
                AliveItems.Add(item);

            return i;
        }

        public override void RemoveRange(int index, int count)
        {
            // A bit inefficient, but so far isn't used in any performance-critical code.
            for (int i = 0; i < count; ++i)
                RemoveAt(index);
        }

        public override void RemoveAt(int index)
        {
            AliveItems.Remove(this[index]);
            base.RemoveAt(index);
            current.RemoveAt(index);
        }

        public override void Clear()
        {
            AliveItems.Clear();
            base.Clear();
            current.Clear();
        }
    }
}
