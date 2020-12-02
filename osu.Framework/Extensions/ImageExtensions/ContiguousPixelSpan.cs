// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Buffers;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Extensions.ImageExtensions
{
    public readonly ref struct ContiguousPixelSpan<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// The span of pixels.
        /// </summary>
        public readonly Span<TPixel> Span;

        private readonly IMemoryOwner<TPixel>? owner;

        internal ContiguousPixelSpan(Span<TPixel> span, IMemoryOwner<TPixel>? owner)
        {
            Span = span;
            this.owner = owner;
        }

        public void Dispose()
        {
            owner?.Dispose();
        }
    }
}
