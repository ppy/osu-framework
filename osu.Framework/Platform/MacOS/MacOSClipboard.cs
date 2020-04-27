// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        private readonly NSPasteboard generalPasteboard = NSPasteboard.GeneralPasteboard();

        public override string GetText()
        {
            NSArray classArray = NSArray.ArrayWithObject(Class.Get("NSString"));

            if (!generalPasteboard.CanReadObjectForClasses(classArray, null)) return string.Empty;

            var result = generalPasteboard.ReadObjectsForClasses(classArray, null);

            var objects = result?.ToArray();

            if (objects?.Length > 0 && objects[0] != IntPtr.Zero)
                return Cocoa.FromNSString(objects[0]);

            return string.Empty;
        }

        public override void SetText(string selectedText)
        {
            generalPasteboard.ClearContents();
            generalPasteboard.WriteObjects(NSArray.ArrayWithObject(Cocoa.ToNSString(selectedText)));
        }
    }
}
