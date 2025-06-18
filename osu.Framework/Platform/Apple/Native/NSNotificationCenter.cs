// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Apple.Native
{
    internal static class NSNotificationCenter
    {
        private static readonly IntPtr sel_default_center = Selector.Get("defaultCenter");
        private static readonly IntPtr sel_add_observer = Selector.Get("addObserver:selector:name:object:");

        internal static IntPtr Handle = Interop.SendIntPtr(Class.Get("NSNotificationCenter"), sel_default_center);

        internal static readonly IntPtr WINDOW_DID_ENTER_FULL_SCREEN = Interop.GetStringConstant(Interop.AppKitLibrary, "NSWindowDidEnterFullScreenNotification");
        internal static readonly IntPtr WINDOW_DID_EXIT_FULL_SCREEN = Interop.GetStringConstant(Interop.AppKitLibrary, "NSWindowDidExitFullScreenNotification");

        internal static void AddObserver(IntPtr target, IntPtr selector, IntPtr name, IntPtr obj) =>
            Interop.SendVoid(Handle, sel_add_observer, target, selector, name, obj);
    }
}
