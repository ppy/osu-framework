// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using CoreGraphics;
using Foundation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using UIKit;

namespace osu.Framework.iOS.Graphics.Textures
{
    public class IOSTextureLoaderStore : TextureLoaderStore
    {
        public IOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (var uiImage = UIImage.LoadFromData(NSData.FromStream(stream)))
            {
                if (uiImage == null) throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                int width = (int)uiImage.Size.Width;
                int height = (int)uiImage.Size.Height;

                // TODO: Use pool/memory when builds success with Xamarin.
                // Probably at .NET Core 3.1 time frame.
                byte[] data = new byte[width * height * 4];
                using (CGBitmapContext textureContext = new CGBitmapContext(data, width, height, 8, width * 4, CGColorSpace.CreateDeviceRGB(), CGImageAlphaInfo.PremultipliedLast))
                    textureContext.DrawImage(new CGRect(0, 0, width, height), uiImage.CGImage);

                var image = Image.LoadPixelData<TPixel>(data, width, height);

                return image;
            }
        }
    }
}
