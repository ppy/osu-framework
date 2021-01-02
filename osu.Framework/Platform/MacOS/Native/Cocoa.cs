// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Cocoa
    {
        internal const string LIB_DL = "libSystem.dylib";
        internal const string LIB_APPKIT = "/System/Library/Frameworks/AppKit.framework/AppKit";
        internal const string LIB_OBJ_C = "/usr/lib/libobjc.dylib";
        internal const string LIB_CORE_GRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/Versions/Current/CoreGraphics";

        internal const int RTLD_NOW = 2;

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int arg);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, ulong ulong1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1, int int1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern uint SendUint(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, uint arg);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2, IntPtr intPtr3, IntPtr intPtr4);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend_fpret")]
        public static extern float SendFloat_i386(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern double SendFloat_x64(IntPtr receiver, IntPtr selector);

        public static float SendFloat(IntPtr receiver, IntPtr selector) => IntPtr.Size == 4 ? SendFloat_i386(receiver, selector) : (float)SendFloat_x64(receiver, selector);

        public static IntPtr AppKitLibrary;

        [DllImport(LIB_CORE_GRAPHICS, EntryPoint = "CGCursorIsVisible")]
        public static extern bool CGCursorIsVisible();

        [DllImport(LIB_CORE_GRAPHICS, EntryPoint = "CGEventSourceFlagsState")]
        public static extern ulong CGEventSourceFlagsState(int stateID);

        [DllImport(LIB_DL)]
        private static extern IntPtr dlsym(IntPtr handle, string name);

        [DllImport(LIB_DL)]
        private static extern IntPtr dlopen(string fileName, int flags);

        static Cocoa()
        {
            AppKitLibrary = dlopen(LIB_APPKIT, RTLD_NOW);
        }

        private static readonly IntPtr sel_c_string_using_encoding = Selector.Get("cStringUsingEncoding:");

        public static string FromNSString(IntPtr handle) => Marshal.PtrToStringUni(SendIntPtr(handle, sel_c_string_using_encoding, (uint)NSStringEncoding.Unicode));

        public static unsafe IntPtr ToNSString(string str)
        {
            if (str == null)
                return IntPtr.Zero;

            fixed (char* ptrFirstChar = str)
            {
                var handle = SendIntPtr(Class.Get("NSString"), Selector.Get("alloc"));
                return SendIntPtr(handle, Selector.Get("initWithCharacters:length:"), (IntPtr)ptrFirstChar, str.Length);
            }
        }

        public static IntPtr GetStringConstant(IntPtr handle, string symbol)
        {
            IntPtr ptr = dlsym(handle, symbol);
            return ptr == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(ptr);
        }
    }
}
