// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class NSApplication
    {
        internal static IntPtr Handle;

        private static readonly IntPtr sel_shared_application = Selector.Get("sharedApplication");
        private static readonly IntPtr sel_set_presentation_options = Selector.Get("setPresentationOptions:");
        private static readonly IntPtr sel_presentation_options = Selector.Get("presentationOptions");

        static NSApplication()
        {
            IntPtr nsappClass = Class.Get("NSApplication");
            Handle = Cocoa.SendIntPtr(nsappClass, sel_shared_application);
        }

        internal static NSApplicationPresentationOptions PresentationOptions
        {
            get => (NSApplicationPresentationOptions)Cocoa.SendUint(Handle, sel_presentation_options);
            set => Cocoa.SendVoid(Handle, sel_set_presentation_options, (uint)value);
        }
    }
}
