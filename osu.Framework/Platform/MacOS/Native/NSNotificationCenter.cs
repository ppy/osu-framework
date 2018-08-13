// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class NSNotificationCenter
    {
        internal static IntPtr Handle;

        internal static readonly IntPtr WINDOW_DID_ENTER_FULL_SCREEN;
        internal static readonly IntPtr WINDOW_DID_EXIT_FULL_SCREEN;

        private static readonly IntPtr sel_default_center = Selector.Get("defaultCenter");
        private static readonly IntPtr sel_add_observer = Selector.Get("addObserver:selector:name:object:");

        static NSNotificationCenter()
        {
            IntPtr nsncClass = Class.Get("NSNotificationCenter");
            Handle = Cocoa.SendIntPtr(nsncClass, sel_default_center);

            WINDOW_DID_ENTER_FULL_SCREEN = Cocoa.GetStringConstant(Cocoa.AppKitLibrary, "NSWindowDidEnterFullScreenNotification");
            WINDOW_DID_EXIT_FULL_SCREEN = Cocoa.GetStringConstant(Cocoa.AppKitLibrary, "NSWindowDidExitFullScreenNotification");
        }

        internal static void AddObserver(IntPtr target, IntPtr selector, IntPtr name, IntPtr obj) =>
            Cocoa.SendVoid(Handle, sel_add_observer, target, selector, name, obj);
    }
}
