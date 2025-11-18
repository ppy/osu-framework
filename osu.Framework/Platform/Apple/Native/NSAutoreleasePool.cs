// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

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

        [MustDisposeResource]
        public static NSAutoreleasePool Init()
            => new NSAutoreleasePool(Interop.SendIntPtr(Interop.SendIntPtr(class_pointer, sel_alloc), sel_init));

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                Interop.SendIntPtr(Handle, sel_drain);
        }
    }
}
