// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Low level class for queueing texture uploads to the GPU.
    /// </summary>
    public class TextureUpload : ITextureUpload
    {
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

        // ReSharper disable once MergeConditionalExpression (can't merge; compile error)
        public ReadOnlySpan<Rgba32> Data => image != null ? image.GetPixelSpan() : Span<Rgba32>.Empty;

        public int Width => image?.Width ?? 0;

        public int Height => image?.Height ?? 0;

        /// <summary>
        /// The backing texture. A handle is kept to avoid early GC.
        /// </summary>
        private readonly Image<Rgba32> image;

        /// <summary>
        /// Create an upload from a <see cref="TextureUpload"/>. This is the preferred method.
        /// </summary>
        /// <param name="image">The texture to upload.</param>
        public TextureUpload(Image<Rgba32> image)
        {
            this.image = image;
        }

        /// <summary>
        /// Create an upload from an arbitrary image stream.
        /// </summary>
        /// <param name="stream">The image content.</param>
        public TextureUpload(Stream stream)
            : this(Image.Load(stream))
        {
        }

        /// <summary>
        /// Create an empty upload. Used by <see cref="FrameBuffer"/> for initialisation.
        /// </summary>
        internal TextureUpload()
        {
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                image?.Dispose();
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
