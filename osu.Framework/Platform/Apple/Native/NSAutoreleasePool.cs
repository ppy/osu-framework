// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly struct NSAutoreleasePool : IDisposable
    {
        internal IntPtr Handle { get; }

        internal NSAutoreleasePool(IntPtr handle)
        {
            Handle = handle;
        }

        private static readonly IntPtr class_pointer = Class.Get("NSAutoreleasePool");
        private static readonly IntPtr sel_alloc = Selector.Get("alloc");
        private static readonly IntPtr sel_init = Selector.Get("init");
        private static readonly IntPtr sel_drain = Selector.Get("drain");

        public static NSAutoreleasePool Init()
        {
            var pool = alloc();
            Interop.SendIntPtr(pool.Handle, sel_init);
            return pool;
        }

        private static NSAutoreleasePool alloc() => new NSAutoreleasePool(Interop.SendIntPtr(class_pointer, sel_alloc));

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                Interop.SendIntPtr(Handle, sel_drain);
        }
    }
}
