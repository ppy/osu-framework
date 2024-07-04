// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Buffers;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class MemoryAllocatorTextureUpload : ITextureUpload
    {
        public Span<Rgba32> RawData => memoryOwner.Memory.Span;

        public ReadOnlySpan<Rgba32> Data => RawData;

        private readonly IMemoryOwner<Rgba32> memoryOwner;

        public int Level { get; set; }

        public virtual PixelFormat Format => PixelFormat.Rgba;

        public RectangleI Bounds { get; set; }

        /// <summary>
        /// Create an empty raw texture with an efficient shared memory backing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="memoryAllocator">The source to retrieve memory from. Shared default is used if null.</param>
        public MemoryAllocatorTextureUpload(int width, int height, MemoryAllocator memoryAllocator = null)
        {
            memoryOwner = (memoryAllocator ?? SixLabors.ImageSharp.Configuration.Default.MemoryAllocator).Allocate<Rgba32>(width * height);
        }

        #region IDisposable Support

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (disposed)
                return;

            memoryOwner?.Dispose();

            disposed = true;
        }

        #endregion
    }
}
