﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureByteArray : IRawTexture
    {
        protected byte[] Bytes;
        protected Rectangle Dimensions;

        public PixelFormat PixelFormat { get; protected set; }

        public ITextureLocker ObtainLock() => new TextureLockerByteArray(Bytes);

        public int Width => Dimensions.Width;
        public int Height => Dimensions.Height;

        protected RawTextureByteArray()
        {
        }

        public RawTextureByteArray(byte[] bytes, Rectangle dimensions, PixelFormat format = PixelFormat.Rgba)
        {
            PixelFormat = format;
            Bytes = bytes;
            Dimensions = dimensions;
        }

        #region IDisposable Support

        public void Dispose()
        {
            // nothing to do.
        }

        #endregion
    }
}
