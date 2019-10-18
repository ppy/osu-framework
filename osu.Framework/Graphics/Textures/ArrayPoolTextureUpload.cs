// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class ArrayPoolTextureUpload : ITextureUpload
    {
        public Span<Rgba32> RawData => memoryOwner.Memory.Span;

        public ReadOnlySpan<Rgba32> Data => RawData;

        private readonly IMemoryOwner<Rgba32> memoryOwner;

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

        /// <summary>
        /// Create an empty raw texture with an efficient shared memory backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        public ArrayPoolTextureUpload(int width, int height)
        {
            memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<Rgba32>(width * height);
        }

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public bool HasBeenUploaded => disposed;

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                memoryOwner.Dispose();
            }
        }

        ~ArrayPoolTextureUpload()
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
