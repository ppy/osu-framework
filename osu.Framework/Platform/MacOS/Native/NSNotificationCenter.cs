// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct NSNotificationCenter
    {
        internal static IntPtr Handle;

        internal static readonly IntPtr WindowDidEnterFullScreen;
        internal static readonly IntPtr WindowDidExitFullScreen;

        private static readonly IntPtr selDefaultCenter = Selector.Get("defaultCenter");
        private static readonly IntPtr selAddObserver = Selector.Get("addObserver:selector:name:object:");

        static NSNotificationCenter()
        {
            IntPtr nsncClass = Class.Get("NSNotificationCenter");
            Handle = Cocoa.SendIntPtr(nsncClass, selDefaultCenter);

            WindowDidEnterFullScreen = Cocoa.GetStringConstant(Cocoa.AppKitLibrary, "NSWindowDidEnterFullScreenNotification");
            WindowDidExitFullScreen = Cocoa.GetStringConstant(Cocoa.AppKitLibrary, "NSWindowDidExitFullScreenNotification");
        }

        internal static void AddObserver(IntPtr target, IntPtr selector, IntPtr name, IntPtr obj) =>
            Cocoa.SendVoid(Handle, selAddObserver, target, selector, name, obj);
    }
}
