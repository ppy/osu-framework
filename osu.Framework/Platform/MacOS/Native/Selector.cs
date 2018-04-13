// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Selector
    {
        private static readonly Type type_selector = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Selector");
        private static readonly MethodInfo method_selector_get = type_selector.GetMethod("Get");

        public static IntPtr Get(string name)
        {
            return (IntPtr)method_selector_get.Invoke(null, new object[] { name });
        }
    }
}
