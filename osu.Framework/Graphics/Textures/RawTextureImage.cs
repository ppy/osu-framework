using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureImage : RawTexture
    {
        public readonly Image<Rgba32> Data;

        /// <summary>
        /// Create a raw texture from an arbitrary image stream.
        /// </summary>
        /// <param name="stream">The image content.</param>
        public RawTextureImage(Stream stream)
            : this(Image.Load(stream))
        {
        }

        public RawTextureImage(Image<Rgba32> data)
        {
            Data = data;
            Width = Data.Width;
            Height = Data.Height;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Data.Dispose();
        }

        public override ReadOnlySpan<Rgba32> GetImageData() => Data.GetPixelSpan();
    }
}