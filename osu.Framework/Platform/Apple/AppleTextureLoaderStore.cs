// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform.Apple.Native;
using osu.Framework.Platform.Apple.Native.Accelerate;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Platform.Apple
{
    internal abstract class AppleTextureLoaderStore : TextureLoaderStore
    {
        protected AppleTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected unsafe Image<TPixel> ImageFromCGImage<TPixel>(CGImage cgImage)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            int width = (int)cgImage.Width;
            int height = (int)cgImage.Height;

            var format = new vImage_CGImageFormat
            {
                BitsPerComponent = 8,
                BitsPerPixel = 32,
                ColorSpace = CGColorSpace.CreateDeviceRGB(),
                // notably, macOS & iOS generally use premultiplied alpha when rendering image to pixels via CGBitmapContext or otherwise,
                // but vImage offers rendering as straight alpha by specifying Last instead of PremultipliedLast.
                BitmapInfo = (CGBitmapInfo)CGImageAlphaInfo.Last,
                Decode = null,
                RenderingIntent = CGColorRenderingIntent.Default,
            };

            vImage_Buffer accImage = default;

            // perform initial call to retrieve preferred alignment and bytes-per-row values for the given image dimensions.
            nuint alignment = (nuint)vImage.Init(&accImage, (uint)height, (uint)width, 32, vImage_Flags.NoAllocate);
            Debug.Assert(alignment > 0);

            // allocate aligned memory region to contain image pixel data.
            nuint bytesPerRow = accImage.BytesPerRow;
            nuint bytesCount = bytesPerRow * accImage.Height;
            accImage.Data = (byte*)NativeMemory.AlignedAlloc(bytesCount, alignment);

            var result = vImage.InitWithCGImage(&accImage, &format, null, cgImage.Handle, vImage_Flags.NoAllocate);
            Debug.Assert(result == vImage_Error.NoError);

            var image = new Image<TPixel>(width, height);

            for (int i = 0; i < height; i++)
            {
                var imageRow = image.DangerousGetPixelRowMemory(i);
                var dataRow = new ReadOnlySpan<TPixel>(&accImage.Data[(int)bytesPerRow * i], width);
                dataRow.CopyTo(imageRow.Span);
            }

            NativeMemory.AlignedFree(accImage.Data);
            return image;
        }
    }
}
