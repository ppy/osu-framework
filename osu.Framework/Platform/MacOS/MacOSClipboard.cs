// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Platform.MacOS.Native;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        private readonly NSPasteboard generalPasteboard = NSPasteboard.GeneralPasteboard();

        public override string GetText() => Cocoa.FromNSString(getFromPasteboard(Class.Get("NSString")));

        public override Image<TPixel> GetImage<TPixel>() => Cocoa.FromNSImage<TPixel>(getFromPasteboard(Class.Get("NSImage")));

        public override void SetText(string selectedText) => setToPasteboard(Cocoa.ToNSString(selectedText));

        public override bool SetImage(Image image) => setToPasteboard(Cocoa.ToNSImage(image));

        private IntPtr getFromPasteboard(IntPtr @class)
        {
            NSArray classArray = NSArray.ArrayWithObject(@class);

            if (!generalPasteboard.CanReadObjectForClasses(classArray, null))
                return IntPtr.Zero;

            var result = generalPasteboard.ReadObjectsForClasses(classArray, null);
            var objects = result?.ToArray();

            return objects?.Length > 0 ? objects[0] : IntPtr.Zero;
        }

        private bool setToPasteboard(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return false;

            generalPasteboard.ClearContents();
            generalPasteboard.WriteObjects(NSArray.ArrayWithObject(handle));
            return true;
        }
    }
}
