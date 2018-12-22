// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Loads images from a <see cref="Stream"/>.
    /// Default functionality is to use <see cref="Image.Load(Stream)"/>, but supports dropping in an alternate implementation.
    /// </summary>
    public class ImageLoader : IImageLoader
    {
        /// <summary>
        /// The currently assigned <see cref="IImageLoader"/> implementation.
        /// </summary>
        internal static IImageLoader Implementation;

        /// <summary>
        /// Defers image loading to the currently assigned <see cref="IImageLoader"/> implementation.
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <typeparam name="TPixel">The pixel format.</typeparam>
        public static Image<TPixel> Load<TPixel>(Stream stream) where TPixel : struct, IPixel<TPixel> => Implementation.FromStream<TPixel>(stream);

        /// <summary>
        /// Default functionality that reads images using <see cref="Image.Load(Stream)"/>.
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <typeparam name="TPixel">The pixel format.</typeparam>
        public Image<TPixel> FromStream<TPixel>(Stream stream) where TPixel : struct, IPixel<TPixel> => Image.Load<TPixel>(stream);
    }
}
