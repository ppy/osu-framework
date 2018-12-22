// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using osu.Framework.Graphics.Textures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UIKit;

namespace osu.Framework.iOS.Graphics
{
    /// <summary>
    /// An image loader for iOS that uses a temporary <see cref="UIImage"/> and <see cref="CGBitmapContext"/>.
    /// It is currently much faster than ImageSharp's implementation due to lack of SIMD support on iOS and Android.
    /// </summary>
    public class IOSImageLoader : IImageLoader
    {
        public Image<TPixel> FromStream<TPixel>(Stream stream)
            where TPixel : struct, IPixel<TPixel>
        {
            var uiImage = UIImage.LoadFromData(NSData.FromStream(stream));
            int width = (int)uiImage.Size.Width;
            int height = (int)uiImage.Size.Height;
            IntPtr data = Marshal.AllocHGlobal(width * height * 4);

            using (CGBitmapContext textureContext = new CGBitmapContext(data, width, height, 8, width * 4, uiImage.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                textureContext.DrawImage(new CGRect(0, 0, width, height), uiImage.CGImage);

            var pixels = new byte[width * height * 4];
            Marshal.Copy(data, pixels, 0, pixels.Length);
            Marshal.FreeHGlobal(data);

            // NOTE: this will probably only be correct for Rgba32, will need to look into other pixel formats
            return Image.LoadPixelData<TPixel>(pixels, width, height);
        }
    }
}
