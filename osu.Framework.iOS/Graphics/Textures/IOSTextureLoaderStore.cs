// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.IO;
using CoreGraphics;
using CoreImage;
using Foundation;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

                using (var ciImage = CIImage.FromData(nativeData))
                {
                    if (ciImage == null) throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                    int width = (int)ciImage.Extent.Width;
                    int height = (int)ciImage.Extent.Height;

                    RgbaVector[] dataInVectors = ArrayPool<RgbaVector>.Shared.Rent(width * height);
                    byte[] dataInBytes = ArrayPool<byte>.Shared.Rent(width * height * 4);

                    fixed (RgbaVector* ptr = dataInVectors)
                    {
                        using (var context = CIContext.Create())
                            context.RenderToBitmap(ciImage, (IntPtr)ptr, width * 16, ciImage.Extent, CIImage.FormatRGBAf, CGColorSpace.CreateDeviceRGB());
                    }

                    // unapply alpha pre-multiplication resulted by CGBitmapContext.
                    for (int i = 0; i < dataInVectors.Length; i++)
                    {
                        RgbaVector v = dataInVectors[i];
                        dataInBytes[i * 4 + 0] = (byte)Math.Round(v.A == 0 ? 0 : v.R / v.A * 255);
                        dataInBytes[i * 4 + 1] = (byte)Math.Round(v.A == 0 ? 0 : v.G / v.A * 255);
                        dataInBytes[i * 4 + 2] = (byte)Math.Round(v.A == 0 ? 0 : v.B / v.A * 255);
                        dataInBytes[i * 4 + 3] = (byte)Math.Round(v.A * 255);
                    }

                    var image = Image.LoadPixelData<TPixel>(dataInBytes, width, height);

                    ArrayPool<byte>.Shared.Return(dataInBytes);
                    ArrayPool<RgbaVector>.Shared.Return(dataInVectors);
                    return image;
                }
            }
        }
    }
}
