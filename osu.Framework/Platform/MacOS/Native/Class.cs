// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Class
    {
        [DllImport(Cocoa.LIB_OBJ_C)]
        private static extern IntPtr class_getName(IntPtr handle);

        [DllImport(Cocoa.LIB_OBJ_C)]
        private static extern IntPtr class_replaceMethod(IntPtr classHandle, IntPtr selector, IntPtr method, string types);

        [DllImport(Cocoa.LIB_OBJ_C)]
        private static extern IntPtr objc_getClass(string name);

        public static IntPtr Get(string name)
        {
            var id = objc_getClass(name);
            if (id == IntPtr.Zero)
                throw new ArgumentException("Unknown class: " + name);
            return id;
        }

        public static void RegisterMethod(IntPtr handle, Delegate d, string selector, string typeString) =>
            class_replaceMethod(handle, Selector.Get(selector), Marshal.GetFunctionPointerForDelegate(d), typeString);
    }
}
