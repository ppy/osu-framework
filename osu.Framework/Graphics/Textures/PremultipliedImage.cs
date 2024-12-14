// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Colour;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace osu.Framework.Graphics.Textures
{
    public class PremultipliedImage : IDisposable
    {
        /// <summary>
        /// The underlying image in <see cref="Image{TPixel}"/> form.
        /// </summary>
        public readonly Image<Rgba32> Premultiplied;

        private PremultipliedImage(Image<Rgba32> premultiplied)
        {
            Premultiplied = premultiplied;
        }

        public PremultipliedImage(int width, int height)
            : this(new Image<Rgba32>(width, height))
        {
        }

        public PremultipliedImage(int width, int height, PremultipliedColour colour)
            : this(new Image<Rgba32>(width, height, new Rgba32(colour.Premultiplied.R, colour.Premultiplied.G, colour.Premultiplied.B, colour.Premultiplied.A)))
        {
        }

        public PremultipliedImage Clone() => FromPremultiplied(Premultiplied.Clone());

        public void Dispose()
        {
            Premultiplied.Dispose();
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
