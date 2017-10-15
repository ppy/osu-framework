// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct NSPasteboard
    {
        internal IntPtr Handle { get; private set; }

        private static IntPtr classPointer = Class.Get("NSPasteboard");
        private static IntPtr selGeneralPasteboard = Selector.Get("generalPasteboard");
        private static IntPtr selClearContents = Selector.Get("clearContents");
        private static IntPtr selCanReadObjectForClasses = Selector.Get("canReadObjectForClasses:options:");
        private static IntPtr selReadObjectsForClasses = Selector.Get("readObjectsForClasses:options:");
        private static IntPtr selWriteObjects = Selector.Get("writeObjects:");

        internal NSPasteboard(IntPtr handle)
        {
            Handle = handle;
        }

        internal static NSPasteboard GeneralPasteboard() => new NSPasteboard(Cocoa.SendIntPtr(classPointer, selGeneralPasteboard));

        internal int ClearContents() => Cocoa.SendInt(Handle, selClearContents);

        internal bool CanReadObjectForClasses(NSArray classArray, NSDictionary? optionDict) => Cocoa.SendBool(Handle, selCanReadObjectForClasses, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);

        internal NSArray? ReadObjectsForClasses(NSArray classArray, NSDictionary? optionDict)
        {
            var result = Cocoa.SendIntPtr(Handle, selReadObjectsForClasses, classArray.Handle, optionDict?.Handle ?? IntPtr.Zero);
            return result == IntPtr.Zero ? (NSArray?)null : new NSArray(result);
        }

        internal bool WriteObjects(NSArray objects) => Cocoa.SendBool(Handle, selWriteObjects, objects.Handle);
    }
}
