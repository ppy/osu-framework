// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUploadByteArray : TextureUploadRawTexture
    {
        public readonly byte[] Data;

        private readonly BufferStack<byte> bufferStack;

        public TextureUploadByteArray(Size size, BufferStack<byte> bufferStack = null) : this(bufferStack?.ReserveBuffer(size.Width * size.Height * 4) ?? new byte[size.Width * size.Height * 4], size)
        {
            this.bufferStack = bufferStack;
        }

        public TextureUploadByteArray(byte[] data, Size size) : base(new RawTextureBytes(data, new Rectangle(Point.Empty, size)))
        {
            Data = data;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            bufferStack?.FreeBuffer(Data);
        }
    }
}
