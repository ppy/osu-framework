// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using osu.Framework.Allocation;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUpload : IDisposable
    {
        private static BufferStack<byte> globalBufferStack = new BufferStack<byte>(10);

        public int Level;
        public PixelFormat Format = PixelFormat.Rgba;
        public Rectangle Bounds;
        public readonly byte[] Data;

        private BufferStack<byte> bufferStack;

        public TextureUpload(int size, BufferStack<byte> bufferStack = null)
        {
            this.bufferStack = bufferStack ?? globalBufferStack;
            Data = this.bufferStack.ReserveBuffer(size);
        }

        public TextureUpload(byte[] data)
        {
            Data = data;
        }

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                bufferStack?.FreeBuffer(Data);
            }
        }

        ~TextureUpload()
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
