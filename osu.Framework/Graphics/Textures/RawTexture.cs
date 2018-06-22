// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.Allocation;
using SixLabors.ImageSharp;
using PixelFormat = OpenTK.Graphics.ES30.PixelFormat;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Texture data in a raw byte format.
    /// </summary>
    public class RawTexture : IDisposable
    {
        /// <summary>
        /// The width of the texture data.
        /// </summary>
        public int Width;

        /// <summary>
        /// They height of the texture data.
        /// </summary>
        public int Height;

        /// <summary>
        /// The pixel format of the texture data.
        /// </summary>
        public readonly PixelFormat PixelFormat = PixelFormat.Rgba;

        /// <summary>
        /// The texture data.
        /// </summary>
        public byte[] Data;

        private readonly BufferStack<byte> bufferStack;

        /// <summary>
        /// Create a raw texture from an arbitrary image stream.
        /// </summary>
        /// <param name="stream">The image content.</param>
        public RawTexture(Stream stream)
        {
            using (var img = Image.Load(stream))
            {
                Width = img.Width;
                Height = img.Height;
                Data = img.SavePixelData();
            }
        }

        /// <summary>
        /// Create an empty raw texture with an optional <see cref="BufferStack{T}"/>. backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="bufferStack">The buffer stack to retrieve the byte[] from.</param>
        public RawTexture(int width, int height, BufferStack<byte> bufferStack = null)
        {
            int size = width * height * 4;

            Width = width;
            Height = height;

            if (bufferStack != null)
            {
                this.bufferStack = bufferStack;
                Data = this.bufferStack.ReserveBuffer(size);
            }
            else
            {
                Data = new byte[size];
            }
        }

        /// <summary>
        /// Create an empty raw texture with an optional <see cref="BufferStack{T}"/>. backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The heightof the texture.</param>
        /// <param name="data">The raw texture data.</param>
        public RawTexture(int width, int height, byte[] data)
        {
            int size = width * height * 4;

            if (size != data.Length)
                throw new InvalidOperationException("Provided data does not match dimensions");

            Data = data;
            Width = width;
            Height = height;
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                bufferStack?.FreeBuffer(Data);
            }
        }

        ~RawTexture()
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
