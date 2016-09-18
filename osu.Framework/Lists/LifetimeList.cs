//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace osu.Framework.Lists
{
    public class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        private double lastTime;

        public LifetimeList(IComparer<T> comparer)
            : base(comparer)
        {
        }

        public IEnumerable<T> Current
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].IsAlive)
                        yield return base[i];
                }
            }
        }

        public List<T> Update(double time)
        {
            List<T> removedObjects = new List<T>();

            for (int i = 0; i < Count; i++)
            {
                var obj = this[i];

                if (obj.IsAlive)
                {
                    if (!obj.IsLoaded)
                        obj.Load();
                }
                else if (obj.RemoveWhenNotAlive)
                {
                    RemoveAt(i--);
                    removedObjects.Add(obj);
                }
            }

            lastTime = time;
            return removedObjects;
        }

        public override int Add(T item)
        {
            if (item.IsAlive && !item.IsLoaded)
                item.Load();

            return base.Add(item);
        }
    }
}
