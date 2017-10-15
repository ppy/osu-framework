// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Class
    {
        static Type typeClass;
        static MethodInfo methodClassGet;
        static MethodInfo methodRegisterMethod;

        static Class()
        {
            typeClass = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Class");
            methodClassGet = typeClass.GetMethod("Get");
            methodRegisterMethod = typeClass.GetMethod("RegisterMethod");
        }

        public static IntPtr Get(string name)
        {
            return (IntPtr)methodClassGet.Invoke(null, new object[] { name });
        }

        public static void RegisterMethod(IntPtr handle, Delegate d, string selector, string typeString)
        {
            methodRegisterMethod.Invoke(null, new object[] { handle, d, selector, typeString });
        }
    }
}
