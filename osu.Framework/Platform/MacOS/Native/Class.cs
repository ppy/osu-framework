// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Class
    {
        private static readonly Type type_class = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Class");
        private static readonly MethodInfo method_class_get = type_class.GetMethod("Get");
        private static readonly MethodInfo method_register_method = type_class.GetMethod("RegisterMethod");

        public static IntPtr Get(string name)
        {
            return (IntPtr)method_class_get.Invoke(null, new object[] { name });
        }

        public static void RegisterMethod(IntPtr handle, Delegate d, string selector, string typeString)
        {
            method_register_method.Invoke(null, new object[] { handle, d, selector, typeString });
        }
    }
}
