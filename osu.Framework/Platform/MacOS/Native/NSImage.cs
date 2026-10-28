// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.Platform.Apple.Native;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSImage : IDisposable
    {
        internal IntPtr Handle { get; }

        internal NSImage(IntPtr handle)
        {
            Handle = handle;
        }

        private static readonly IntPtr class_pointer = Class.Get("NSImage");
        private static readonly IntPtr sel_alloc = Selector.Get("alloc");
        private static readonly IntPtr sel_release = Selector.Get("release");
        private static readonly IntPtr sel_init_with_data = Selector.Get("initWithData:");
        private static readonly IntPtr sel_tiff_representation = Selector.Get("TIFFRepresentation");
        private static readonly IntPtr sel_cg_image_for_proposed_rect = Selector.Get("CGImageForProposedRect:context:hints:");

        internal CGImage CGImage => new CGImage(Interop.SendIntPtr(Handle, sel_cg_image_for_proposed_rect, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));

        internal NSData TiffRepresentation => new NSData(Interop.SendIntPtr(Handle, sel_tiff_representation));

        [MustDisposeResource]
        internal static NSImage InitWithData(NSData data)
            => new NSImage(Interop.SendIntPtr(Interop.SendIntPtr(class_pointer, sel_alloc), sel_init_with_data, data));

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
                Interop.SendVoid(Handle, sel_release);
        }
    }
}
