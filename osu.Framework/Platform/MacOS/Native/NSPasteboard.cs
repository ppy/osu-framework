// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.Apple.Native;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSPasteboard
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSPasteboard");
        private static readonly IntPtr sel_general_pasteboard = Selector.Get("generalPasteboard");
        private static readonly IntPtr sel_clear_contents = Selector.Get("clearContents");
        private static readonly IntPtr sel_can_read_object_for_classes = Selector.Get("canReadObjectForClasses:options:");
        private static readonly IntPtr sel_read_objects_for_classes = Selector.Get("readObjectsForClasses:options:");
        private static readonly IntPtr sel_write_objects = Selector.Get("writeObjects:");

        internal NSPasteboard(IntPtr handle)
        {
            Handle = handle;
        }

        internal static NSPasteboard GeneralPasteboard() => new NSPasteboard(Interop.SendIntPtr(class_pointer, sel_general_pasteboard));

        internal int ClearContents() => Interop.SendInt(Handle, sel_clear_contents);

        internal bool CanReadObjectForClasses(NSArray classArray, NSDictionary? optionDict) =>
            Interop.SendBool(Handle, sel_can_read_object_for_classes, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);

        internal NSArray? ReadObjectsForClasses(NSArray classArray, NSDictionary? optionDict)
        {
            IntPtr result = Interop.SendIntPtr(Handle, sel_read_objects_for_classes, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);
            return result == IntPtr.Zero ? null : new NSArray(result);
        }

        internal bool WriteObjects(NSArray objects) => Interop.SendBool(Handle, sel_write_objects, objects.Handle);
    }
}
