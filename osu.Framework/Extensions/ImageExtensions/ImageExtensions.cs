// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Extensions.ImageExtensions
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Creates a contiguous and read-only span from the pixels of an <see cref="Image{TPixel}"/>.
        /// Useful for retrieving unmanaged pointers to the entire pixel data of the <see cref="Image{TPixel}"/> for marshalling.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="ReadOnlyPixelSpan{TPixel}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <param name="image">The <see cref="Image{TPixel}"/>.</param>
        /// <typeparam name="TPixel">The type of pixels in <paramref name="image"/>.</typeparam>
        /// <returns>The <see cref="ReadOnlyPixelSpan{TPixel}"/>.</returns>
        public static ReadOnlyPixelSpan<TPixel> CreateReadOnlyPixelSpan<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
            => new ReadOnlyPixelSpan<TPixel>(image);

        /// <summary>
        /// Creates a contiguous and read-only memory from the pixels of an <see cref="Image{TPixel}"/>.
        /// Useful for retrieving unmanaged pointers to the entire pixel data of the <see cref="Image{TPixel}"/> for marshalling.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="ReadOnlyPixelMemory{TPixel}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <param name="image">The <see cref="Image{TPixel}"/>.</param>
        /// <typeparam name="TPixel">The type of pixels in <paramref name="image"/>.</typeparam>
        /// <returns>The <see cref="ReadOnlyPixelMemory{TPixel}"/>.</returns>
        public static ReadOnlyPixelMemory<TPixel> CreateReadOnlyPixelMemory<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
            => new ReadOnlyPixelMemory<TPixel>(image);

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
            var allocatedMemory = allocatedOwner.Memory;

            for (int r = 0; r < image.Height; r++)
                image.DangerousGetPixelRowMemory(r).CopyTo(allocatedMemory.Slice(r * image.Width));

            return allocatedOwner;
        }
    }
}
