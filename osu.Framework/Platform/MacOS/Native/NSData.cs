// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSData
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSData");
        private static readonly IntPtr sel_data_with_bytes = Selector.Get("dataWithBytes:length:");
        private static readonly IntPtr sel_bytes = Selector.Get("bytes");
        private static readonly IntPtr sel_length = Selector.Get("length");

        internal NSData(IntPtr handle)
        {
            Handle = handle;
        }

        internal byte[] ToBytes()
        {
            var pointer = Cocoa.SendIntPtr(Handle, sel_bytes);
            int size = Cocoa.SendInt(Handle, sel_length);

            byte[] bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, size);
            return bytes;
        }

        internal static unsafe NSData FromBytes(byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                var handle = Cocoa.SendIntPtr(class_pointer, sel_data_with_bytes, (IntPtr)ptr, (ulong)bytes.LongLength);
                return new NSData(handle);
            }
        }

        public static implicit operator IntPtr(NSData data) => data.Handle;
    }
}
