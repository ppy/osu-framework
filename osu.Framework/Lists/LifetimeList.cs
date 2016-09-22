// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Extensions;

namespace osu.Framework.Lists
{
    public class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        private double lastTime;

        public event Action<T> OnRemoved;

        List<T> current;

        public LifetimeList(IComparer<T> comparer) : base(comparer)
        {
            current = new List<T>();
        }

        public List<T> Current => current;

        /// <summary>
        /// Update this LifetimeList with the provided time value.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>Whether any alive states were changed.</returns>
        public bool Update(double time)
        {
            bool anyAliveChanged = false;

            for (int i = 0; i < Count; i++)
            {
                var obj = this[i];

                if (obj.IsAlive)
                {
                    if (!current.Contains(obj))
                    {
                        current.Add(obj);
                        anyAliveChanged = true;
                    }

                    if (!obj.IsLoaded)
                        obj.Load();
                }
                else
                {
                    if (current.Remove(obj))
                        anyAliveChanged = true;

                    if (obj.RemoveWhenNotAlive)
                    {
                        RemoveAt(i--);
                        OnRemoved?.Invoke(obj);
                    }
                }
            }

            lastTime = time;
            return anyAliveChanged;
        }

        public new int Add(T item)
        {
            if (item.IsAlive && !item.IsLoaded)
                item.Load();
            
            return base.Add(item);
        }

        public new void Clear()
        {
            base.Clear();
            current.Clear();
        }
    }
}
