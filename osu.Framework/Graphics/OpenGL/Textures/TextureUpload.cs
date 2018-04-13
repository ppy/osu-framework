// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public class TextureUpload : IDisposable
    {
        private static readonly BufferStack<byte> global_buffer_stack = new BufferStack<byte>(10);

        public int Level;
        public PixelFormat Format = PixelFormat.Rgba;
        public RectangleI Bounds;
        public readonly byte[] Data;

        private readonly BufferStack<byte> bufferStack;

        public TextureUpload(int size, BufferStack<byte> bufferStack = null)
        {
            this.bufferStack = bufferStack ?? global_buffer_stack;
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
