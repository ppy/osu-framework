// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform.Apple.Native;
using osu.Framework.Platform.MacOS.Native;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSClipboard : Clipboard
    {
        private readonly NSPasteboard generalPasteboard = NSPasteboard.GeneralPasteboard();

        public override string GetText()
        {
            var nsString = new NSString(getFromPasteboard(Class.Get("NSString")));
            return nsString.ToString();
        }

        public override Image<TPixel>? GetImage<TPixel>()
        {
            var nsImage = new NSImage(getFromPasteboard(Class.Get("NSImage")));
            if (nsImage.Handle == IntPtr.Zero)
                return null;

            return Image.Load<TPixel>(nsImage.TiffRepresentation.ToBytes());
        }

        public override void SetText(string text) => setToPasteboard(NSString.FromString(text).Handle);

        public override bool SetImage(Image image)
        {
            using var stream = new MemoryStream();
            image.SaveAsTiff(stream);

            using (NSAutoreleasePool.Init())
            {
                var nsData = NSData.FromBytes(stream.ToArray());
                using var nsImage = NSImage.InitWithData(nsData);
                return setToPasteboard(nsImage.Handle);
            }
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
