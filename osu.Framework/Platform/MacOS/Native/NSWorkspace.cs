// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSWorkspace
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSWorkspace");
        private static readonly IntPtr sel_shared_workspace = Selector.Get("sharedWorkspace");
        private static readonly IntPtr sel_select_file = Selector.Get("selectFile:inFileViewerRootedAtPath:");

        internal NSWorkspace(IntPtr handle)
        {
            Handle = handle;
        }

        internal static NSWorkspace SharedWorkspace() => new NSWorkspace(Cocoa.SendIntPtr(class_pointer, sel_shared_workspace));

        internal bool SelectFile(string file) => Cocoa.SendBool(Handle, sel_select_file, Cocoa.ToNSString(file));
    }
}
