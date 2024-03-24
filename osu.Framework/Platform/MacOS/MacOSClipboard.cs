// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NuGet.Packaging;
using osu.Framework.Platform.MacOS.Native;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        private readonly NSPasteboard generalPasteboard = NSPasteboard.GeneralPasteboard();

        private readonly Dictionary<string, string> customFormatValues = new Dictionary<string, string>();

        public override string? GetText() => Cocoa.FromNSString(getFromPasteboard(Class.Get("NSString")));

        public override Image<TPixel>? GetImage<TPixel>() => Cocoa.FromNSImage<TPixel>(getFromPasteboard(Class.Get("NSImage")));

        public override string? GetCustom(string format)
        {
            return customFormatValues[format];
        }

        private IntPtr getFromPasteboard(IntPtr @class)
        {
            NSArray classArray = NSArray.ArrayWithObject(@class);

            if (!generalPasteboard.CanReadObjectForClasses(classArray, null))
                return IntPtr.Zero;

            var result = generalPasteboard.ReadObjectsForClasses(classArray, null);
            IntPtr[]? objects = result?.ToArray();

            return objects?.Length > 0 ? objects[0] : IntPtr.Zero;
        }

        public override bool SetData(ClipboardData data)
        {
            generalPasteboard.ClearContents();
            customFormatValues.Clear();

            bool success = true;

            if (data.Text != null)
                success &= setToPasteboard(Cocoa.ToNSString(data.Text));

            if (data.Image != null)
                success &= setToPasteboard(Cocoa.ToNSImage(data.Image));

            customFormatValues.AddRange(data.CustomFormatValues);

            return success;
        }

        private bool setToPasteboard(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return false;

            generalPasteboard.WriteObjects(NSArray.ArrayWithObject(handle));
            return true;
        }
    }
}
