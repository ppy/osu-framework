// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Accelerate;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using UIKit;

namespace osu.Framework.iOS.Graphics.Textures
{
    public class IOSTextureLoaderStore : TextureLoaderStore
    {
        public IOSTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected override unsafe Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (var nativeData = NSData.FromStream(stream))
            {
                if (nativeData == null)
                    throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                using (var uiImage = UIImage.LoadFromData(nativeData))
                {
                    if (uiImage == null) throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                    int width = (int)uiImage.Size.Width;
                    int height = (int)uiImage.Size.Height;

                    var format = new vImage_CGImageFormat
                    {
                        BitsPerComponent = 8,
                        BitsPerPixel = 32,
                        ColorSpace = CGColorSpace.CreateDeviceRGB().Handle,
                        // notably, iOS generally uses premultiplied alpha when rendering image to pixels via CGBitmapContext or otherwise,
                        // but vImage offers using straight alpha directly without any conversion from our side (by specifying Last instead of PremultipliedLast).
                        BitmapInfo = (CGBitmapFlags)CGImageAlphaInfo.Last,
                        Decode = null,
                        RenderingIntent = CGColorRenderingIntent.Default,
                    };

                    vImageBuffer accelerateImage = default;

                    // perform initial call to retrieve preferred alignment and bytes-per-row values for the given image dimensions.
                    nuint alignment = (nuint)vImageBuffer_Init(&accelerateImage, (uint)height, (uint)width, 32, vImageFlags.NoAllocate);
                    Debug.Assert(alignment > 0);

                    // allocate aligned memory region to contain image pixel data.
                    int bytesPerRow = accelerateImage.BytesPerRow;
                    int bytesCount = bytesPerRow * accelerateImage.Height;
                    accelerateImage.Data = (IntPtr)NativeMemory.AlignedAlloc((nuint)bytesCount, alignment);

                    var result = vImageBuffer_InitWithCGImage(&accelerateImage, &format, null, uiImage.CGImage!.Handle, vImageFlags.NoAllocate);
                    Debug.Assert(result == vImageError.NoError);

                    var image = new Image<TPixel>(width, height);
                    byte* data = (byte*)accelerateImage.Data;

                    for (int i = 0; i < height; i++)
                    {
                        var imageRow = image.DangerousGetPixelRowMemory(i);
                        var dataRow = new ReadOnlySpan<TPixel>(&data[bytesPerRow * i], width);
                        dataRow.CopyTo(imageRow.Span);
                    }

                    NativeMemory.AlignedFree(accelerateImage.Data.ToPointer());
                    return image;
                }
            }
        }

        #region Accelerate API

        [DllImport(Constants.AccelerateLibrary)]
        private static extern unsafe vImageError vImageBuffer_Init(vImageBuffer* buf, uint height, uint width, uint pixelBits, vImageFlags flags);

        [DllImport(Constants.AccelerateLibrary)]
        private static extern unsafe vImageError vImageBuffer_InitWithCGImage(vImageBuffer* buf, vImage_CGImageFormat* format, nfloat* backgroundColour, NativeHandle image, vImageFlags flags);

        // ReSharper disable once InconsistentNaming
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct vImage_CGImageFormat
        {
            public uint BitsPerComponent;
            public uint BitsPerPixel;
            public NativeHandle ColorSpace;
            public CGBitmapFlags BitmapInfo;
            public uint Version;
            public nfloat* Decode;
            public CGColorRenderingIntent RenderingIntent;
        }

        #endregion
    }
}
