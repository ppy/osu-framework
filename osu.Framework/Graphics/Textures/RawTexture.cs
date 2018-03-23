// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    public interface IRawTexture : IDisposable
    {
        ITextureLocker ObtainLock();

        int Width { get; }
        int Height { get; }

        PixelFormat PixelFormat { get; }
    }

    public class RawTextureBytes : IRawTexture
    {
        private readonly byte[] bytes;
        private readonly Rectangle dimensions;

        public PixelFormat PixelFormat => PixelFormat.Rgba;

        public ITextureLocker ObtainLock() => new TextureLockerByteArray(bytes);

        public int Width => dimensions.Width;
        public int Height => dimensions.Height;

        public RawTextureBytes(byte[] bytes, Rectangle dimensions)
        {
            this.bytes = bytes;
            this.dimensions = dimensions;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class RawTextureBitmap : IRawTexture
    {
        public PixelFormat PixelFormat { get; }
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

            using (var locker = ObtainLock())
            {
                unsafe
                {
                    //convert from BGRA (System.Drawing) to RGBA
                    //don't need to consider stride because we're in a raw format
                    var src = (byte*)locker.DataPointer;

                    Debug.Assert(src != null);

                    int length = Region.Width * Region.Height;
                    for (int i = 0; i < length; i++)
                    {
                        //BGRA -> RGBA
                        byte temp = src[2];
                        src[2] = src[0];
                        src[0] = temp;

                        src += 4;
                    }
                }
            }
        }

        public RawTextureBitmap(Bitmap bitmap)
            : this(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height))
        {
        }

        public RawTextureBitmap([NotNull] Bitmap bitmap, Rectangle region)
        {
            Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));

            Region = region;
            PixelFormat = PixelFormat.Rgba;
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

    public interface ITextureLocker : IDisposable
    {
        IntPtr DataPointer { get; }
    }

    public class TextureLockerByteArray : ITextureLocker
    {
        private GCHandle handle;

        public IntPtr DataPointer => handle.AddrOfPinnedObject();

        public TextureLockerByteArray(byte[] bytes)
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            handle.Free();
        }
    }

    public class TextureLockerBitmap : ITextureLocker
    {
        private readonly Bitmap bitmap;

        private readonly BitmapData data;

        public IntPtr DataPointer => data.Scan0;

        public TextureLockerBitmap(Bitmap bitmap, Rectangle region)
        {
            this.bitmap = bitmap;
            data = bitmap.LockBits(region, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public void Dispose()
        {
            bitmap.UnlockBits(data);
        }
    }
}
