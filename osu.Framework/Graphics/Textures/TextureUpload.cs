// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StbiSharp;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Low level class for queueing texture uploads to the GPU.
    /// Should be manually disposed if not queued for upload via <see cref="Texture.SetData(ITextureUpload)"/>.
    /// </summary>
    public class TextureUpload : ITextureUpload
    {
        /// <summary>
        /// The target mipmap level to upload into.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The texture format for this upload.
        /// </summary>
        public PixelFormat Format => PixelFormat.Rgba;

        /// <summary>
        /// The target bounds for this upload. If not specified, will assume to be (0, 0, width, height).
        /// </summary>
        public RectangleI Bounds { get; set; }

        /// <summary>
        /// Overview of the texture upload's pixels content. The pixel colours are premultiplied.
        /// </summary>
        public ReadOnlySpan<Rgba32> PremultipliedData => premultipliedPixelMemory.Span;

        public int Width => image?.Width ?? 0;

        public int Height => image?.Height ?? 0;

        /// <summary>
        /// The backing texture. A handle is kept to avoid early GC.
        /// </summary>
        private readonly PremultipliedImage image;

        private ReadOnlyPixelMemory<Rgba32> premultipliedPixelMemory;

        /// <summary>
        /// Create an upload from a <see cref="TextureUpload"/>. This is the preferred method.
        /// </summary>
        /// <param name="image">The texture to upload.</param>
        public TextureUpload(PremultipliedImage image)
        {
            this.image = image;
            premultipliedPixelMemory = image.CreateReadOnlyPremultipliedPixelMemory();
        }

        /// <summary>
        /// Create an upload from an arbitrary image stream.
        /// Note that this bypasses per-platform image loading optimisations.
        /// Use <see cref="TextureLoaderStore"/> as provided from GameHost where possible.
        /// </summary>
        /// <param name="stream">The image content.</param>
        public TextureUpload(Stream stream)
            : this(LoadFromStream(stream))
        {
        }

        private static bool stbiNotFound;

        internal static PremultipliedImage LoadFromStream(Stream stream)
        {
            if (stbiNotFound)
                return PremultipliedImage.FromStraight(Image.Load<Rgba32>(stream));

            long initialPos = stream.Position;

            try
            {
                using (var buffer = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<byte>((int)stream.Length))
                {
                    stream.ReadExactly(buffer.Memory.Span);

                    using (var stbiImage = Stbi.LoadFromMemory(buffer.Memory.Span, 4))
                        return PremultipliedImage.FromStraight(Image.LoadPixelData(MemoryMarshal.Cast<byte, Rgba32>(stbiImage.Data), stbiImage.Width, stbiImage.Height));
                }
            }
            catch (Exception e)
            {
                if (e is DllNotFoundException)
                    stbiNotFound = true;

                Logger.Log($"Texture could not be loaded via STB; falling back to ImageSharp: {e.Message}");
                stream.Position = initialPos;
                return PremultipliedImage.FromStraight(Image.Load<Rgba32>(stream));
            }
        }

        /// <summary>
        /// Create an empty upload. Used by <see cref="IFrameBuffer"/> for initialisation.
        /// </summary>
        internal TextureUpload()
        {
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

            image?.Dispose();
            premultipliedPixelMemory.Dispose();

            disposed = true;
        }

        #endregion
    }
}
