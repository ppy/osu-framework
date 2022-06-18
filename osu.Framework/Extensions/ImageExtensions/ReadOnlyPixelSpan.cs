// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Extensions.ImageExtensions
{
    public readonly ref struct ReadOnlyPixelSpan<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// The span of pixels.
        /// </summary>
        public readonly ReadOnlySpan<TPixel> Span;

        private readonly IMemoryOwner<TPixel>? owner;

        internal ReadOnlyPixelSpan(Image<TPixel> image)
        {
            if (image.DangerousTryGetSinglePixelMemory(out var memory))
            {
                owner = null;
                Span = memory.Span;
            }
            else
            {
                owner = image.CreateContiguousMemory();
                Span = owner.Memory.Span;
            }
        }

        public void Dispose()
        {
            owner?.Dispose();
        }
    }
}
