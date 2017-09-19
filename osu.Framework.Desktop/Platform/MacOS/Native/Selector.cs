// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Desktop.Platform.MacOS.Native
{
    internal class Selector
    {
        static Type typeSelector;
        static MethodInfo methodSelectorGet;

        static Selector()
        {
            typeSelector = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Selector");
            methodSelectorGet = typeSelector.GetMethod("Get");
        }

        public static IntPtr Get(string name)
        {
            return (IntPtr)methodSelectorGet.Invoke(null, new object[] { name });
        }
    }
}
