//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUpload : IDisposable
    {
        private static TextureBufferStack TextureBufferStack = new TextureBufferStack(10);

        public int Level;
        public PixelFormat Format;
        public Rectangle Bounds;
        public readonly byte[] Data;
        private bool shouldFreeBuffer;

        public TextureUpload(int size)
        {
            Data = TextureBufferStack.ReserveBuffer(size);
            shouldFreeBuffer = true;
        }

        public TextureUpload(byte[] data)
        {
            Data = data;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (shouldFreeBuffer)
                {
                    TextureBufferStack.FreeBuffer(Data);
                }
                disposedValue = true;
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