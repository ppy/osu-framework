// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace osu.Framework.Graphics.Textures
{
    public class TextureLockerBitmap : ITextureLocker
    {
        private readonly Bitmap bitmap;

        private readonly BitmapData data;

        public IntPtr DataPointer => data.Scan0;

        public TextureLockerBitmap(Bitmap bitmap, Rectangle region)
        {
            this.bitmap = bitmap;
            data = bitmap.LockBits(region, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        }

        public void Dispose()
        {
            bitmap.UnlockBits(data);
        }
    }
}
