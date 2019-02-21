// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.InteropServices;
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

        protected unsafe override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (var uiImage = UIImage.LoadFromData(NSData.FromStream(stream)))
            {
                int width = (int)uiImage.Size.Width;
                int height = (int)uiImage.Size.Height;

                IntPtr data = Marshal.AllocHGlobal(width * height * 4);
                using (CGBitmapContext textureContext = new CGBitmapContext(data, width, height, 8, width * 4, uiImage.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                    textureContext.DrawImage(new CGRect(0, 0, width, height), uiImage.CGImage);

                // NOTE: this will probably only be correct for Rgba32, will need to look into other pixel formats
                var image = Image.LoadPixelData<TPixel>(
                    new ReadOnlySpan<byte>(data.ToPointer(), width * height * 4),
                    width, height);

                Marshal.FreeHGlobal(data);
                return image;
            }
        }
    }
}
