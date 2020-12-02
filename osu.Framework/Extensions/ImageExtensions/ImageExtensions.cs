// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Extensions.ImageExtensions
{
    public static class ImageExtensions
    {
        /// <summary>
        /// A stack-only struct containing a contiguous memory buffer from the pixels of an <see cref="Image{TPixel}"/>.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="ContiguousPixelSpan{TPixel}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <param name="image">The <see cref="Image{TPixel}"/>.</param>
        /// <typeparam name="TPixel">The type of pixels in <paramref name="image"/>.</typeparam>
        /// <returns>The <see cref="ContiguousPixelSpan{TPixel}"/>.</returns>
        public static ContiguousPixelSpan<TPixel> GetContiguousPixelSpan<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            if (image.TryGetSinglePixelSpan(out var span))
                return new ContiguousPixelSpan<TPixel>(span, null);

            var contiguousOwner = image.CreateContiguousMemory();
            return new ContiguousPixelSpan<TPixel>(contiguousOwner.Memory.Span, contiguousOwner);
        }

        /// <summary>
        /// A struct that can be stored, containing a contiguous memory buffer from the pixels of an <see cref="Image{TPixel}"/>.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="ContiguousPixelMemory{TPixel}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <param name="image">The <see cref="Image{TPixel}"/>.</param>
        /// <typeparam name="TPixel">The type of pixels in <paramref name="image"/>.</typeparam>
        /// <returns>The <see cref="ContiguousPixelMemory{TPixel}"/>.</returns>
        public static ContiguousPixelMemory<TPixel> GetContiguousPixelMemory<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
            => new ContiguousPixelMemory<TPixel>(image);

        /// <summary>
        /// Creates a new contiguous memory buffer from the pixels in an <see cref="Image{TPixel}"/>.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IMemoryOwner{T}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <param name="image">The <see cref="Image{TPixel}"/>.</param>
        /// <typeparam name="TPixel">The type of pixels in <paramref name="image"/>.</typeparam>
        /// <returns>The <see cref="IMemoryOwner{T}"/>, containing the contiguous pixel memory.</returns>
        internal static IMemoryOwner<TPixel> CreateContiguousMemory<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var allocatedOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<TPixel>(image.Width * image.Height);
            var allocatedSpan = allocatedOwner.Memory.Span;

            for (int r = 0; r < image.Height; r++)
                image.GetPixelRowSpan(r).CopyTo(allocatedSpan.Slice(r * image.Width));

            return allocatedOwner;
        }
    }
}
