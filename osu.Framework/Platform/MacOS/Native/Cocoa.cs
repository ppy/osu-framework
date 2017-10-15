// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Cocoa
    {
        internal const string LibObjC = "/usr/lib/libobjc.dylib";

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, int arg);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static int SendInt(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static bool SendBool(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static bool SendBool(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public extern static void SendVoid(IntPtr receiver, IntPtr selector, IntPtr ptr1, IntPtr ptr2);

        private static Type typeCocoa;
        private static MethodInfo methodCocoaFromNSString;
        private static MethodInfo methodCocoaToNSString;
        private static MethodInfo methodCocoaGetStringConstant;

        public static IntPtr AppKitLibrary;
        public static IntPtr FoundationLibrary;

        static Cocoa()
        {
            typeCocoa = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Cocoa");
            methodCocoaFromNSString = typeCocoa.GetMethod("FromNSString");
            methodCocoaToNSString = typeCocoa.GetMethod("ToNSString");
            methodCocoaGetStringConstant = typeCocoa.GetMethod("GetStringConstant");

            AppKitLibrary = (IntPtr)typeCocoa.GetField("AppKitLibrary").GetValue(null);
            FoundationLibrary = (IntPtr)typeCocoa.GetField("FoundationLibrary").GetValue(null);
        }

        public static string FromNSString(IntPtr handle)
        {
            return (string)methodCocoaFromNSString.Invoke(null, new object[] { handle });
        }

        public static IntPtr ToNSString(string str)
        {
            return (IntPtr)methodCocoaToNSString.Invoke(null, new object[] { str });
        }

        public static IntPtr GetStringConstant(IntPtr handle, string symbol)
        {
            return (IntPtr)methodCocoaGetStringConstant.Invoke(null, new object[] { handle, symbol });
        }
    }
}
