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
            /*var uiImage = BitmapFactory.DecodeStream(stream);
            int[] bytes = new int[uiImage.Width * uiImage.Height];
            uiImage.GetPixels(bytes, 0, uiImage.RowBytes, 0, 0, uiImage.Width, uiImage.Height);
            int width = uiImage.Width;
            int height = uiImage.Height;
            IntPtr data = Marshal.AllocHGlobal(width * height * 4);

            using (Canvas canvas = new Canvas(uiImage))
                canvas.DrawBitmap(uiImage, new Matrix(), new Paint());

            byte[] pixels = new byte[width * height * 4];

            Marshal.Copy(data, bytes, 0, pixels.Length);
            Marshal.FreeHGlobal(data);

            return Image.LoadPixelData<TPixel>(pixels, width, height);*/
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
