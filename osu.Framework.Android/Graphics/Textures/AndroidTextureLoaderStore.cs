// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using System;
using System.IO;

namespace osu.Framework.Android.Graphics.Textures
{
    internal class AndroidTextureLoaderStore : TextureLoaderStore
    {
        public AndroidTextureLoaderStore(IResourceStore<byte[]> store) : base(store)
        {
        }

        protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            using (var bitmap = BitmapFactory.DecodeStream(stream))
            {
                var pixels = new int[bitmap.Width * bitmap.Height];
                bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);
                byte[] result = new byte[pixels.Length * sizeof(int)];
                Buffer.BlockCopy(pixels, 0, result, 0, result.Length);
                return Image.LoadPixelData<TPixel>(result, bitmap.Width, bitmap.Height);
            }
        }
    }
}
