// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class BufferStackTextureUpload : ITextureUpload
    {
        public Rgba32[] RawData;

        public ReadOnlySpan<Rgba32> Data => RawData;

        /// <summary>
        /// The target mipmap level to upload into.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The texture format for this upload.
        /// </summary>
        public PixelFormat Format => PixelFormat.Rgba;

        /// <summary>
        /// The target bounds for this upload. If not specified, will assume to be (0, 0, width, height).
        /// </summary>
        public RectangleI Bounds { get; set; }

        private readonly BufferStack<Rgba32> bufferStack;

        /// <summary>
        /// Create an empty raw texture with an optional <see cref="BufferStack{T}"/>. backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="bufferStack">The buffer stack to retrieve the Rgba32[] from.</param>
        public BufferStackTextureUpload(int width, int height, BufferStack<Rgba32> bufferStack)
        {
            this.bufferStack = bufferStack;
            RawData = bufferStack.ReserveBuffer(width * height);
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                bufferStack?.FreeBuffer(RawData);
            }
        }

        ~BufferStackTextureUpload()
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
