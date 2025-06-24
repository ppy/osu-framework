// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using SharpFNT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A glyph store which caches font sprite sheets as raw pixels to disk on first use.
    /// </summary>
    /// <remarks>
    /// This results in memory efficient lookups with good performance on solid state backed devices.
    /// Consider <see cref="TimedExpiryGlyphStore"/> if disk IO is limited and memory usage is not an issue.
    /// </remarks>
    public class RawCachingGlyphStore : GlyphStore
    {
        /// <summary>
        /// A storage backing to be used for storing decompressed glyph sheets.
        /// </summary>
        internal Storage? CacheStorage { get; set; }

        private readonly Dictionary<string, Stream> pageStreamHandles = new Dictionary<string, Stream>();

        private readonly Dictionary<int, PageInfo> pageLookup = new Dictionary<int, PageInfo>();

        public RawCachingGlyphStore(ResourceStore<byte[]> store, string? assetName = null, IResourceStore<TextureUpload>? textureLoader = null, bool coloured = false)
            : base(store, assetName, textureLoader, coloured)
        {
        }

        protected override TextureUpload LoadCharacter(Character character)
        {
            if (CacheStorage == null)
                throw new InvalidOperationException($"{nameof(CacheStorage)} should be set before requesting characters.");

            // Use simple global locking for the time being.
            // If necessary, a per-lookup-key (page number) locking mechanism could be implemented similar to TextureStore.
            lock (pageLookup)
            {
                if (!pageLookup.TryGetValue(character.Page, out var pageInfo))
                    pageInfo = createCachedPageInfo(character.Page);

                return createTextureUpload(character, pageInfo);
            }
        }

        private PageInfo createCachedPageInfo(int page)
        {
            Debug.Assert(CacheStorage != null);

            string filename = GetFilenameForPage(page);

            using (var stream = Store.GetStream(filename))
            {
                // The md5 of the original (compressed png) content.
                string streamMd5 = stream.ComputeMD5Hash();

                // The md5 of the access filename, including font name and page number.
                string filenameMd5 = filename.ComputeMD5Hash();

                string accessFilename = $"{filenameMd5}#{streamMd5}";

                // Finding an existing file validates that the file both exists on disk, and was generated for the correct font.
                // It doesn't guarantee that the generated cache file is in a good state.
                string? existing = CacheStorage.GetFiles(string.Empty, $"{accessFilename}*").FirstOrDefault();

                if (existing != null)
                {
                    // Filename format is "filenameHashMD5#contentHashMD5#width#height"
                    string[] split = existing.Split('#');

                    if (split.Length == 4
                        && int.TryParse(split[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                        && int.TryParse(split[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                    {
                        // Sanity check that the length of the file is expected, based on the width and height.
                        // If we ever see corrupt files in the wild, this should be changed to a full md5 check. Hopefully it will never happen.
                        using (var testStream = CacheStorage.GetStream(existing))
                        {
                            if (testStream.Length == width * height * (Coloured ? 4 : 1)) // 4 bytes per pixel for RGBA
                            {
                                return pageLookup[page] = new PageInfo
                                {
                                    Size = new Size(width, height),
                                    Filename = existing
                                };
                            }
                        }
                    }
                }

                using (var convert = GetPageImage(page))
                {
                    var source = convert.Data;

                    // ensure any stale cached versions are deleted.
                    foreach (string f in CacheStorage.GetFiles(string.Empty, $"{filenameMd5}*"))
                        CacheStorage.Delete(f);

                    accessFilename += FormattableString.Invariant($"#{convert.Width}#{convert.Height}");

                    if (Coloured)
                    {
                        // Handle coloured fonts - store full RGBA data
                        using (var buffer = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<Rgba32>(convert.Width * convert.Height))
                        {
                            var rgbaOutput = buffer.Memory.Span;
                            for (int i = 0; i < rgbaOutput.Length; i++)
                                rgbaOutput[i] = source[i];

                            using (var outStream = CacheStorage.CreateFileSafely(accessFilename))
                            {
                                var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(buffer.Memory.Span);
                                outStream.Write(bytes);
                            }
                        }
                    }
                    else
                    {
                        // Handle monochrome fonts - store only alpha channel
                        using (var buffer = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<byte>(convert.Width * convert.Height))
                        {
                            var byteOutput = buffer.Memory.Span;
                            for (int i = 0; i < byteOutput.Length; i++)
                                byteOutput[i] = source[i].A;

                            using (var outStream = CacheStorage.CreateFileSafely(accessFilename))
                            {
                                outStream.Write(buffer.Memory.Span);
                            }
                        }
                    }

                    return pageLookup[page] = new PageInfo
                    {
                        Size = new Size(convert.Width, convert.Height),
                        Filename = accessFilename
                    };
                }
            }
        }

        private TextureUpload createTextureUpload(Character character, PageInfo page)
        {
            Debug.Assert(CacheStorage != null);

            int pageWidth = page.Size.Width;

            // Calculate bytes needed based on coloured mode
            int bytesPerPixel = Coloured ? 4 : 1; // 4 bytes for RGBA, 1 byte for alpha only
            int characterByteRegion = pageWidth * character.Height * bytesPerPixel;
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(characterByteRegion);

            try
            {
                var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, character.Width, character.Height);

                if (!pageStreamHandles.TryGetValue(page.Filename, out var source))
                    source = pageStreamHandles[page.Filename] = CacheStorage.GetStream(page.Filename);

                // consider to use System.IO.RandomAccess in .NET 6
                source.Seek(pageWidth * character.Y * bytesPerPixel, SeekOrigin.Begin);
                source.ReadExactly(readBuffer.AsSpan(0, characterByteRegion));

                // the spritesheet may have unused pixels trimmed
                int readableHeight = Math.Min(character.Height, page.Size.Height - character.Y);
                int readableWidth = Math.Min(character.Width, pageWidth - character.X);

                for (int y = 0; y < character.Height; y++)
                {
                    var pixelRowMemory = image.DangerousGetPixelRowMemory(y);
                    var span = pixelRowMemory.Span;
                    int readOffset = y * pageWidth + character.X;

                    for (int x = 0; x < character.Width; x++)
                    {
                        if (x < readableWidth && y < readableHeight)
                        {
                            if (Coloured)
                            {
                                // Use original RGBA data from cached color font texture
                                var rgbaBuffer = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Rgba32>(readBuffer.AsSpan(0, characterByteRegion));
                                span[x] = rgbaBuffer[readOffset + x];
                            }
                            else
                            {
                                // Use alpha data and set to white for monochrome fonts
                                byte alpha = readBuffer[readOffset + x];
                                span[x] = new Rgba32(255, 255, 255, alpha);
                            }
                        }
                        else
                        {
                            // Only use transparent pixels for areas outside the readable bounds
                            span[x] = new Rgba32(255, 255, 255, 0);
                        }
                    }
                }

                return new TextureUpload(image);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (pageStreamHandles.IsNotNull())
            {
                foreach (var h in pageStreamHandles)
                    h.Value.Dispose();
            }
        }

        private record PageInfo
        {
            public string Filename { get; set; } = string.Empty;
            public Size Size { get; set; }
        }
    }
}
