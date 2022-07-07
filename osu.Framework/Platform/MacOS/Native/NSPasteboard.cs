// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

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

        internal static NSPasteboard GeneralPasteboard() => new NSPasteboard(Cocoa.SendIntPtr(class_pointer, sel_general_pasteboard));

        internal int ClearContents() => Cocoa.SendInt(Handle, sel_clear_contents);

        internal bool CanReadObjectForClasses(NSArray classArray, NSDictionary? optionDict) =>
            Cocoa.SendBool(Handle, sel_can_read_object_for_classes, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);

        internal NSArray? ReadObjectsForClasses(NSArray classArray, NSDictionary? optionDict)
        {
            var result = Cocoa.SendIntPtr(Handle, sel_read_objects_for_classes, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);
            return result == IntPtr.Zero ? null : new NSArray(result);
        }

        internal bool WriteObjects(NSArray objects) => Cocoa.SendBool(Handle, sel_write_objects, objects.Handle);
    }
}
