﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        internal NSPasteboard GeneralPasteboard = NSPasteboard.GeneralPasteboard();

        public override string GetText()
        {
            NSArray classArray = NSArray.ArrayWithObject(Class.Get("NSString"));
            if (GeneralPasteboard.CanReadObjectForClasses(classArray, null))
            {
                var result = GeneralPasteboard.ReadObjectsForClasses(classArray, null);
                var objects = result?.ToArray() ?? new IntPtr[0];
                if (objects.Length > 0 && objects[0] != IntPtr.Zero)
                    return Cocoa.FromNSString(objects[0]);
            }
            return string.Empty;
        }

        public override void SetText(string selectedText)
        {
            GeneralPasteboard.ClearContents();
            GeneralPasteboard.WriteObjects(NSArray.ArrayWithObject(Cocoa.ToNSString(selectedText)));
        }
    }
}
