// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Graphics;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Android.Graphics.Textures
{
    internal class AndroidTextureLoaderStore : TextureLoaderStore
    {
        public AndroidTextureLoaderStore(IResourceStore<byte[]> store) : base(store)
        {
        }

        protected override Image<TPixel> ImageFromStream<TPixel>(Stream stream)
        {
            var uiImage = BitmapFactory.DecodeStream(stream);
            int width = uiImage.Width;
            int height = uiImage.Height;
            IntPtr data = Marshal.AllocHGlobal(width * height * 4);

            using (Canvas canvas = new Canvas(uiImage))
                canvas.DrawBitmap(uiImage, new Rect(0, 0, width, height), new RectF(0, 0, width, height), new Paint());

            var pixels = new byte[width * height * 4];
            Marshal.Copy(data, pixels, 0, pixels.Length);
            Marshal.FreeHGlobal(data);

            return Image.LoadPixelData<TPixel>(pixels, width, height);
        }
    }
}
