// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Reflection;
using osu.Framework.Graphics;

namespace osu.Framework.Caching
{
    public class HandleInputCache
    {
        private readonly ConcurrentDictionary<Type, bool> cachedValues = new ConcurrentDictionary<Type, bool>();

        public bool Get(Drawable drawable)
        {
            var type = drawable.GetType();
            var cached = cachedValues.TryGetValue(type, out var value);

            if (!cached)
            {
                foreach (var inputMethod in drawable.InputMethods)
                {
                    var isOverridden = type.GetMethod(inputMethod, BindingFlags.Instance | BindingFlags.NonPublic).DeclaringType != typeof(Drawable);
                    if (isOverridden)
                    {
                        cachedValues.TryAdd(type, true);
                        return true;
                    }
                }
                cachedValues.TryAdd(type, value = false);
            }

            return value;
        }
    }
}
