// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly struct NSString
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSString");
        private static readonly IntPtr sel_string_with_characters = Selector.Get("stringWithCharacters:length:");
        private static readonly IntPtr sel_utf8_string = Selector.Get("UTF8String");

        internal NSString(IntPtr handle)
        {
            Handle = handle;
        }

        public override string ToString() => Marshal.PtrToStringUTF8(Interop.SendIntPtr(Handle, sel_utf8_string))!;

        internal static unsafe NSString FromString(string str)
        {
            fixed (char* strPtr = str)
                return new NSString(Interop.SendIntPtr(class_pointer, sel_string_with_characters, (IntPtr)strPtr, str.Length));
        }
    }
}
