// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Lists
{
    public class WeakList<T> : List<WeakReference<T>>
        where T : class
    {
        public void Add(T obj) => Add(new WeakReference<T>(obj));

        public void ForEachAlive(Action<T> action)
        {
            int index = 0;
            while (index < Count)
            {
                T obj;
                if (this[index].TryGetTarget(out obj))
                {
                    action(obj);
                    index++;
                }
                else
                    RemoveAt(index);
            }
        }
    }
}
