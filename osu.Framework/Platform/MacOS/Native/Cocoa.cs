﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Cocoa
    {
        internal const string LIB_OBJ_C = "/usr/lib/libobjc.dylib";
        internal const string LIB_CORE_GRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/Versions/Current/CoreGraphics";

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int arg);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern uint SendUint(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern int SendInt(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern int SendInt(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, uint arg);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LIB_OBJ_C, EntryPoint = "objc_msgSend")]
        public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr intPtr1, IntPtr intPtr2, IntPtr intPtr3, IntPtr intPtr4);

        private static readonly Type type_cocoa = typeof(osuTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Cocoa");
        private static readonly MethodInfo method_cocoa_from_ns_string = type_cocoa.GetMethod("FromNSString");
        private static readonly MethodInfo method_cocoa_to_ns_string = type_cocoa.GetMethod("ToNSString");
        private static readonly MethodInfo method_cocoa_get_string_constant = type_cocoa.GetMethod("GetStringConstant");

        public static IntPtr AppKitLibrary;
        public static IntPtr FoundationLibrary;

        [DllImport(LIB_CORE_GRAPHICS, EntryPoint = "CGCursorIsVisible")]
        public static extern bool CGCursorIsVisible();

        static Cocoa()
        {
            AppKitLibrary = (IntPtr)type_cocoa.GetField("AppKitLibrary").GetValue(null);
            FoundationLibrary = (IntPtr)type_cocoa.GetField("FoundationLibrary").GetValue(null);
        }

        public static string FromNSString(IntPtr handle)
        {
            return (string)method_cocoa_from_ns_string.Invoke(null, new object[] { handle });
        }

        public static IntPtr ToNSString(string str)
        {
            return (IntPtr)method_cocoa_to_ns_string.Invoke(null, new object[] { str });
        }

        public static IntPtr GetStringConstant(IntPtr handle, string symbol)
        {
            return (IntPtr)method_cocoa_get_string_constant.Invoke(null, new object[] { handle, symbol });
        }
    }
}
