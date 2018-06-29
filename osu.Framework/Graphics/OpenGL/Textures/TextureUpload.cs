// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// Low level class for queueing texture uploads to the GPU.
    /// </summary>
    public class TextureUpload : IDisposable
    {
        /// <summary>
        /// The target mipmap level to upload into.
        /// </summary>
        public int Level;

        /// <summary>
        /// The texture format for this upload.
        /// </summary>
        public PixelFormat Format = PixelFormat.Rgba;

        /// <summary>
        /// The target bounds for this upload. If not specified, will assume to be (0, 0, width, height).
        /// </summary>
        public RectangleI Bounds;

        /// <summary>
        /// The upload data.
        /// </summary>
        public readonly byte[] Data;

        /// <summary>
        /// The backing texture. A handle is kept to avoid early GC.
        /// </summary>
        private readonly RawTexture texture;

        /// <summary>
        /// Create an upload with arbitrary raw data.
        /// </summary>
        /// <param name="data"></param>
        public TextureUpload(byte[] data)
        {
            Data = data;
        }

        /// <summary>
        /// Create an upload from a <see cref="RawTexture"/>. This is the preferred method.
        /// </summary>
        /// <param name="texture">The texture to upload.</param>
        public TextureUpload(RawTexture texture)
        {
            this.texture = texture;
            Data = texture.Data;
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                texture?.Dispose();
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
