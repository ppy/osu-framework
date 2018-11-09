// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ObjCRuntime;
using System.Runtime.InteropServices;

namespace osu.Framework.iOS.Native
{
    internal static class Swizzling
    {
        internal const string LIB_OBJ_C = "/usr/lib/libobjc.dylib";

        [DllImport(LIB_OBJ_C, EntryPoint = "class_getInstanceMethod")]
        public static extern IntPtr ClassGetInstanceMethod(IntPtr classHandle, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "imp_implementationWithBlock")]
        public static extern IntPtr ImpImplementationWithBlock(ref BlockLiteral block);

        [DllImport(LIB_OBJ_C, EntryPoint = "method_setImplementation")]
        public static extern void MethodSetImplementation(IntPtr method, IntPtr imp);

        public delegate void SwizzleDelegate(IntPtr block, IntPtr self);
        public delegate void SwizzleDelegateInt(IntPtr block, IntPtr self, int arg1);
        public delegate void SwizzleDelegateIntPtr(IntPtr block, IntPtr self, IntPtr arg1);

        public static void SwizzleMethod(IntPtr classHandle, IntPtr selector, SwizzleDelegate swizzleDelegate) => internalSwizzleMethod(classHandle, selector, swizzleDelegate);
        public static void SwizzleMethod(IntPtr classHandle, IntPtr selector, SwizzleDelegateInt swizzleDelegate) => internalSwizzleMethod(classHandle, selector, swizzleDelegate);
        public static void SwizzleMethod(IntPtr classHandle, IntPtr selector, SwizzleDelegateIntPtr swizzleDelegate) => internalSwizzleMethod(classHandle, selector, swizzleDelegate);

        private static void internalSwizzleMethod(IntPtr classHandle, IntPtr selector, Delegate swizzleDelegate)
        {
            var method = ClassGetInstanceMethod(classHandle, selector);
            var blockValue = new BlockLiteral();
            blockValue.SetupBlock(swizzleDelegate, null);
            var imp = ImpImplementationWithBlock(ref blockValue);
            MethodSetImplementation(method, imp);
        }
    }
}
