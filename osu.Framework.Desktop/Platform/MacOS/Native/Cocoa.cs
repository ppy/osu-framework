// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;

namespace osu.Framework.Desktop.Platform.MacOS.Native
{
    internal class Cocoa
    {
        static Type typeCocoa;
        static MethodInfo methodCocoaSendIntPtr1;
        static MethodInfo methodCocoaSendIntPtr2;
        static MethodInfo methodCocoaSendInt;
        static MethodInfo methodCocoaFromNSString;
        static MethodInfo methodCocoaToNSString;
        static MethodInfo methodCocoaGetStringConstant;
        static FieldInfo fieldAppKitLibrary;
        static FieldInfo fieldFoundationLibrary;

        public static IntPtr AppKitLibrary;
        public static IntPtr FoundationLibrary;

        static Cocoa()
        {
            typeCocoa = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "Cocoa");
            methodCocoaSendIntPtr1 = typeCocoa.GetMethod("SendIntPtr", new Type[] { typeof(IntPtr), typeof(IntPtr) });
            methodCocoaSendIntPtr2 = typeCocoa.GetMethod("SendIntPtr", new Type[] { typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) });
            methodCocoaSendInt = typeCocoa.GetMethod("SendInt");
            methodCocoaFromNSString = typeCocoa.GetMethod("FromNSString");
            methodCocoaToNSString = typeCocoa.GetMethod("ToNSString");
            methodCocoaGetStringConstant = typeCocoa.GetMethod("GetStringConstant");
            fieldAppKitLibrary = typeCocoa.GetField("AppKitLibrary");
            fieldFoundationLibrary = typeCocoa.GetField("FoundationLibrary");

            AppKitLibrary = (IntPtr)fieldAppKitLibrary.GetValue(null);
            FoundationLibrary = (IntPtr)fieldFoundationLibrary.GetValue(null);
        }

        public static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector)
        {
            return (IntPtr)methodCocoaSendIntPtr1.Invoke(null, new object[] { receiver, selector });
        }

        public static IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr intPtr1)
        {
            return (IntPtr)methodCocoaSendIntPtr2.Invoke(null, new object[] { receiver, selector, intPtr1 });
        }

        public static int SendInt(IntPtr receiver, IntPtr selector)
        {
            return (int)methodCocoaSendInt.Invoke(null, new object[] { receiver, selector });
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
