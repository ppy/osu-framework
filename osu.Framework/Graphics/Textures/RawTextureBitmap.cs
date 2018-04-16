// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.IO;
using JetBrains.Annotations;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureBitmap : IRawTexture
    {
        public PixelFormat PixelFormat { get; } = PixelFormat.Rgba;
        public readonly Bitmap Bitmap;
        public Rectangle Region;

        public int Width => Region.Width;
        public int Height => Region.Height;

        public ITextureLocker ObtainLock() => new TextureLockerBitmap(Bitmap, Region);

        private readonly bool disposeBitmap;

        public RawTextureBitmap(Stream stream)
            : this(new Bitmap(stream))
        {
            disposeBitmap = true;

            PixelFormat = (PixelFormat)32993; // GL_BGRA
        }

        public RawTextureBitmap(Bitmap bitmap)
            : this(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height))
        {
        }

        public RawTextureBitmap([NotNull] Bitmap bitmap, Rectangle region)
        {
            Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
            Region = region;
        }

        public RawTextureBitmap GetSubregion(Rectangle rectangle) => new RawTextureBitmap(Bitmap, rectangle);

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (disposeBitmap) Bitmap?.Dispose();
            }
        }

        ~RawTextureBitmap()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
