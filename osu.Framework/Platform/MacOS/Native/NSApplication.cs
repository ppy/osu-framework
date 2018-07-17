// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct NSApplication
    {
        internal static IntPtr Handle;

        private static readonly IntPtr selSharedApplication = Selector.Get("sharedApplication");
        private static readonly IntPtr selSetPresentationOptions = Selector.Get("setPresentationOptions:");
        private static readonly IntPtr selPresentationOptions = Selector.Get("presentationOptions");

        static NSApplication()
        {
            IntPtr nsappClass = Class.Get("NSApplication");
            Handle = Cocoa.SendIntPtr(nsappClass, selSharedApplication);
        }

        internal static NSApplicationPresentationOptions PresentationOptions
        {
            get => (NSApplicationPresentationOptions)Cocoa.SendUint(Handle, selPresentationOptions);
            set => Cocoa.SendVoid(Handle, selSetPresentationOptions, (uint)value);
        }
    }
}
