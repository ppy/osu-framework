// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Colour;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace osu.Framework.Graphics.Textures
{
    public class PremultipliedImage : IDisposable
    {
        /// <summary>
        /// Gets the image width in px units.
        /// </summary>
        public int Width => premultipliedImage.Width;

        /// <summary>
        /// Gets the image height in px units.
        /// </summary>
        public int Height => premultipliedImage.Height;

        private readonly Image<Rgba32> premultipliedImage;

        private PremultipliedImage(Image<Rgba32> premultipliedImage)
        {
            this.premultipliedImage = premultipliedImage;
        }

        public PremultipliedImage(int width, int height)
            : this(new Image<Rgba32>(width, height))
        {
        }

        public PremultipliedImage(int width, int height, PremultipliedColour colour)
            : this(new Image<Rgba32>(width, height, colour.ToPremultipliedRgba32()))
        {
        }

        /// <summary>
        /// Gets or sets the pixel at the specified position.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        public PremultipliedColour this[int x, int y]
        {
            get => PremultipliedColour.FromPremultiplied(premultipliedImage[x, y]);
            set => premultipliedImage[x, y] = value.ToPremultipliedRgba32();
        }

        /// <summary>
        /// Creates a contiguous and read-only memory from the pixels of a <see cref="PremultipliedImage"/>.
        /// Useful for retrieving unmanaged pointers to the entire pixel data of the <see cref="PremultipliedImage"/> for marshalling.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="ReadOnlyPixelMemory{Rgba32}"/> must be disposed when usage is finished.
        /// </remarks>
        /// <returns>The <see cref="ReadOnlyPixelMemory{Rgba32}"/>.</returns>
        public ReadOnlyPixelMemory<Rgba32> CreateReadOnlyPremultipliedPixelMemory()
            => premultipliedImage.CreateReadOnlyPixelMemory();

        /// <summary>
        /// Gets the representation of the premultiplied pixels as <see cref="Span{Rgba32}"/> of contiguous memory
        /// at row <paramref name="rowIndex"/> beginning from the first pixel on that row.
        /// </summary>
        /// <param name="rowIndex">The row.</param>
        /// <returns>The <see cref="Span{Rgba32}"/></returns>
        public Memory<Rgba32> DangerousGetPremultipliedPixelRowMemory(int rowIndex)
            => premultipliedImage.DangerousGetPixelRowMemory(rowIndex);

        /// <summary>
        /// Sets a pixel at the specified position with a premultiplied <see cref="Rgba32"/> colour.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        /// <param name="rgba32">The premultiplied <see cref="Rgba32"/> colour.</param>
        public void SetPremultipliedRgba32(int x, int y, Rgba32 rgba32) => premultipliedImage[x, y] = rgba32;

        /// <summary>
        /// Clones the current image.
        /// </summary>
        /// <returns>Returns a new image with all the same metadata as the original.</returns>
        public PremultipliedImage Clone() => FromPremultiplied(premultipliedImage.Clone());

        public void Dispose()
        {
            premultipliedImage.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="PremultipliedImage"/> from a non-premultiplied source <see cref="Image{Rgba32}"/> by converting colours to premultiplied.
        /// </summary>
        /// <param name="image">The non-premultiplied source image.</param>
        public static PremultipliedImage FromStraight(Image<Rgba32> image)
        {
            var premultiplied = image.Clone();
            premultiplied.Mutate(p => p.ProcessPixelRowsAsVector4(r =>
            {
                foreach (ref var pixel in r)
                {
                    pixel.X *= pixel.W;
                    pixel.Y *= pixel.W;
                    pixel.Z *= pixel.W;
                }
            }));

            return new PremultipliedImage(premultiplied);
        }

        /// <summary>
        /// Creates a <see cref="PremultipliedImage"/> from a premultiplied source <see cref="Image{Rgba32}"/>. No conversion is done in this method.
        /// </summary>
        /// <param name="image">The premultiplied source image.</param>
        public static PremultipliedImage FromPremultiplied(Image<Rgba32> image) => new PremultipliedImage(image);
    }
}
