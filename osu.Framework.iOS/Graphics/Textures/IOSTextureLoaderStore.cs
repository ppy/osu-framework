// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Foundation;
using osu.Framework.IO.Stores;
using osu.Framework.Platform.Apple;
using SixLabors.ImageSharp;
using UIKit;

namespace osu.Framework.iOS.Graphics.Textures
{
    internal class IOSTextureLoaderStore : AppleTextureLoaderStore
    {
        public IOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected override unsafe Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (new NSAutoreleasePool())
            {
                int length = (int)(stream.Length - stream.Position);
                var nativeData = NSMutableData.FromLength(length);

                var bytesSpan = new Span<byte>(nativeData.MutableBytes.ToPointer(), length);
                stream.ReadExactly(bytesSpan);

                using var uiImage = UIImage.LoadFromData(nativeData);
                if (uiImage == null)
                    throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                var cgImage = new Platform.Apple.Native.CGImage(uiImage.CGImage!.Handle);
                return ImageFromCGImage<TPixel>(cgImage);
            }
        }
    }
}
