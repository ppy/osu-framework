//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using OpenTK.Graphics.ES20;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUpload : IDisposable
    {
        private static BufferStack globalBufferStack = new BufferStack(10);

        public int Level;
        public PixelFormat Format = PixelFormat.Rgba;
        public Rectangle Bounds;
        public readonly byte[] Data;

        private BufferStack bufferStack;

        public TextureUpload(int size, BufferStack bufferStack = null)
        {
            this.bufferStack = bufferStack == null ? globalBufferStack : bufferStack;
            Data = this.bufferStack.ReserveBuffer(size);
        }

        public TextureUpload(byte[] data)
        {
            Data = data;
        }

        #region IDisposable Support
        private bool disposedValue = false;

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