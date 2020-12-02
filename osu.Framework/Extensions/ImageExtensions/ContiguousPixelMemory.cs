// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Extensions.ImageExtensions
{
    public struct ContiguousPixelMemory<TPixel> : IDisposable
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private Image<TPixel>? image;
        private Memory<TPixel>? memory;
        private IMemoryOwner<TPixel>? owner;

        internal ContiguousPixelMemory(Image<TPixel> image)
        {
            this.image = image;

            memory = null;
            owner = null;
        }

        /// <summary>
        /// The span of pixels.
        /// </summary>
        public Span<TPixel> Span
        {
            get
            {
                // Occurs when this struct has been default-initialised (the struct itself doesn't accept a nullable image).
                if (image == null)
                    return Span<TPixel>.Empty;

                // If the image can be returned without extra contiguous memory allocation.
                if (image.TryGetSinglePixelSpan(out var pixelSpan))
                    return pixelSpan;

                // Only allocate contiguous memory if not already allocated.
                if (memory == null)
                {
                    owner = image.CreateContiguousMemory();
                    memory = owner.Memory;
                }

                return memory.Value.Span;
            }
        }

        public void Dispose()
        {
            owner?.Dispose();
            image = null;
            memory = null;
            owner = null;
        }
    }
}
