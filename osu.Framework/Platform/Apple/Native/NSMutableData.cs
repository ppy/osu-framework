// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly struct NSMutableData
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSMutableData");
        private static readonly IntPtr sel_data_with_length = Selector.Get("dataWithLength:");
        private static readonly IntPtr sel_mutable_bytes = Selector.Get("mutableBytes");

        internal NSMutableData(IntPtr handle)
        {
            Handle = handle;
        }

        internal unsafe byte* MutableBytes => (byte*)Interop.SendIntPtr(Handle, sel_mutable_bytes);

        internal static NSMutableData FromLength(int length)
        {
            IntPtr handle = Interop.SendIntPtr(class_pointer, sel_data_with_length, length);
            return new NSMutableData(handle);
        }
    }
}
