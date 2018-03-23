// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
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
}
