//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace osu.Framework.Lists
{
    class LifetimeList<T> : SortedList<T> where T : IHasLifetime
    {
        public delegate void ElementChangedHandler(T element);

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

        public void Update(double time)
        {
            for (int i = 0; i < Count; i++)
            {
                var obj = this[i];
                bool isAlive = obj.IsAlive;

                if (isAlive)
                {
                    if (!obj.IsLoaded)
                        obj.Load();
                }
                else if (this[i].RemoveWhenNotAlive)
                    RemoveAt(i--);
            }

            lastTime = time;
        }

        public override int Add(T item)
        {
            if (item.IsAlive && !item.IsLoaded)
                item.Load();

            return base.Add(item);
        }
    }
}
