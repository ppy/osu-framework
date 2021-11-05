// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Android.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;

namespace osu.Framework.Android.Graphics.Textures
{
    internal class AndroidTextureLoaderStore : TextureLoaderStore
    {
        public AndroidTextureLoaderStore(IResourceStore<byte[]> store)
            : base(store)
        {
        }

        protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (var bitmap = BitmapFactory.DecodeStream(stream))
            {
                if (bitmap == null) throw new ArgumentException($"{nameof(Image)} could not be created from {nameof(stream)}.");

                int[] pixels = new int[bitmap.Width * bitmap.Height];
                bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);
                byte[] result = new byte[pixels.Length * sizeof(int)];
                Buffer.BlockCopy(pixels, 0, result, 0, result.Length);

                for (int i = 0; i < pixels.Length; i++)
                {
                    (result[i * 4], result[i * 4 + 2]) = (result[i * 4 + 2], result[i * 4]);
                }

                bitmap.Recycle();
                return Image.LoadPixelData<TPixel>(result, bitmap.Width, bitmap.Height);
            }
        }
    }
}
