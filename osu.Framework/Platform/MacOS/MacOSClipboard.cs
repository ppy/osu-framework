// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        internal NSPasteboard generalPasteboard = NSPasteboard.GeneralPasteboard();

        public override string GetText()
        {
            NSArray classArray = NSArray.ArrayWithObject(Class.Get("NSString"));
            if (generalPasteboard.CanReadObjectForClasses(classArray, null))
            {
                var result = generalPasteboard.ReadObjectsForClasses(classArray, null);
                var objects = result?.ToArray() ?? new IntPtr[0];
                if (objects.Length > 0 && objects[0] != IntPtr.Zero)
                    return Cocoa.FromNSString(objects[0]);
            }
            return "";
        }

        public override void SetText(string selectedText)
        {
            generalPasteboard.ClearContents();
            generalPasteboard.WriteObjects(NSArray.ArrayWithObject(Cocoa.ToNSString(selectedText)));
        }
    }
}
