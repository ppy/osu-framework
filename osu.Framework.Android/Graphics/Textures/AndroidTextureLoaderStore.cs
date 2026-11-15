// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Android.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StbiSharp;

namespace osu.Framework.Android.Graphics.Textures
{
    internal class AndroidTextureLoaderStore : TextureLoaderStore
    {
        public AndroidTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        private static bool stbiNotFound;

        protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            if (loadUsingStbi(stream, out Image<TPixel>? loadPixelData))
                return loadPixelData;

            return decodeStream<TPixel>(stream);
        }

        private static bool loadUsingStbi<TPixel>(Stream stream, [NotNullWhen(true)] out Image<TPixel>? image) where TPixel : unmanaged, IPixel<TPixel>
        {
            image = null;
            if (stbiNotFound)
                return false;

            long initialPos = stream.Position;

            try
            {
                using (var buffer = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<byte>((int)stream.Length))
                {
                    stream.ReadExactly(buffer.Memory.Span);

                    using (var stbiImage = Stbi.LoadFromMemory(buffer.Memory.Span, 4))
                    {
                        image = Image.LoadPixelData(MemoryMarshal.Cast<byte, TPixel>(stbiImage.Data), stbiImage.Width, stbiImage.Height);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is DllNotFoundException)
                    stbiNotFound = true;

                Logger.Log($"Texture could not be loaded via STB; falling back to BitmapFactory: {e.Message}");
                stream.Position = initialPos;
            }

            return false;
        }

        private Image<TPixel> decodeStream<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel>
        {
            using (var bitmap = BitmapFactory.DecodeStream(stream))
            {
                if (bitmap == null) throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                int width = bitmap.Width;
                int height = bitmap.Height;

                int[] pixels = new int[width * height];
                bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);
                byte[] result = new byte[pixels.Length * sizeof(int)];
                Buffer.BlockCopy(pixels, 0, result, 0, result.Length);

                for (int i = 0; i < pixels.Length; i++)
                {
                    (result[i * 4], result[i * 4 + 2]) = (result[i * 4 + 2], result[i * 4]);
                }

                bitmap.Recycle();
                return Image.LoadPixelData<TPixel>(result, width, height);
            }
        }
    }
}
