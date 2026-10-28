// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal static partial class Interop
    {
        internal const string LIB_DL = "libSystem.dylib";
        internal const string LIB_APPKIT = "/System/Library/Frameworks/AppKit.framework/AppKit";
        internal const string LIB_OBJ_C = "/usr/lib/libobjc.dylib";
        internal const string LIB_CORE_GRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        internal const string LIB_ACCELERATE = "/System/Library/Frameworks/Accelerate.framework/Accelerate";
        internal const string LIB_CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        internal const int RTLD_NOW = 2;

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int int1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, uint uint1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, ulong ulong1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, int int1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, ulong ulong11);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, ulong ulong1,
                                                [MarshalAs(UnmanagedType.I1)] bool bool1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial int SendInt(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial uint SendUint(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SendBool(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial void SendVoid(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2, IntPtr intPtr3, IntPtr intPtr4);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend_fpret")]
        public static partial float SendFloat_i386(IntPtr receiver, IntPtr selector);

        [LibraryImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static partial double SendFloat_x64(IntPtr receiver, IntPtr selector);

        public static float SendFloat(IntPtr receiver, IntPtr selector) => IntPtr.Size == 4 ? SendFloat_i386(receiver, selector) : (float)SendFloat_x64(receiver, selector);

        public static IntPtr AppKitLibrary;

        [LibraryImport(LIB_DL, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr dlsym(IntPtr handle, string name);

        [LibraryImport(LIB_DL, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr dlopen(string fileName, int flags);

        static Interop()
        {
            AppKitLibrary = dlopen(LIB_APPKIT, RTLD_NOW);
        }

        public static IntPtr GetStringConstant(IntPtr handle, string symbol)
        {
            IntPtr ptr = dlsym(handle, symbol);
            return ptr == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(ptr);
        }
    }
}
