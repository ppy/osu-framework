// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using SharpFNT;
using SixLabors.ImageSharp;
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
        public Storage CacheStorage;

        public RawCachingGlyphStore(ResourceStore<byte[]> store, string assetName = null, IResourceStore<TextureUpload> textureLoader = null)
            : base(store, assetName, textureLoader)
        {
        }

        private readonly Dictionary<int, PageInfo> pageLookup = new Dictionary<int, PageInfo>();

        protected override TextureUpload LoadCharacter(Character character)
        {
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
            string filename = GetFilenameForPage(page);

            using (var stream = Store.GetStream(filename))
            {
                string streamMd5 = stream.ComputeMD5Hash();
                string filenameMd5 = filename.ComputeMD5Hash();

                string accessFilename = $"{filenameMd5}#{streamMd5}";

                string existing = CacheStorage.GetFiles(string.Empty, $"{accessFilename}*").FirstOrDefault();

                if (existing != null)
                {
                    string[] split = existing.Split('#');
                    return pageLookup[page] = new PageInfo
                    {
                        Size = new Size(int.Parse(split[2]), int.Parse(split[3])),
                        Filename = existing
                    };
                }

                using (var convert = GetPageImage(page))
                using (var buffer = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<byte>(convert.Width * convert.Height))
                {
                    var output = buffer.Memory.Span;
                    var source = convert.Data;

                    for (int i = 0; i < output.Length; i++)
                        output[i] = source[i].A;

                    // ensure any stale cached versions are deleted.
                    foreach (string f in CacheStorage.GetFiles(string.Empty, $"{filenameMd5}*"))
                        CacheStorage.Delete(f);

                    accessFilename += $"#{convert.Width}#{convert.Height}";

                    using (var outStream = CacheStorage.GetStream(accessFilename, FileAccess.Write, FileMode.Create))
                        outStream.Write(buffer.Memory.Span);

                    return pageLookup[page] = new PageInfo
                    {
                        Size = new Size(convert.Width, convert.Height),
                        Filename = accessFilename
                    };
                }
            }
        }

        private readonly Dictionary<string, Stream> pageStreamHandles = new Dictionary<string, Stream>();

        private TextureUpload createTextureUpload(Character character, PageInfo page)
        {
            int pageWidth = page.Size.Width;

            if (readBuffer == null || readBuffer.Length < pageWidth * character.Height)
                readBuffer = new byte[pageWidth * character.Height];

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, character.Width, character.Height);

            if (!pageStreamHandles.TryGetValue(page.Filename, out var source))
                source = pageStreamHandles[page.Filename] = CacheStorage.GetStream(page.Filename);

            source.Seek(pageWidth * character.Y, SeekOrigin.Begin);
            source.Read(readBuffer, 0, pageWidth * character.Height);

            // the spritesheet may have unused pixels trimmed
            int readableHeight = Math.Min(character.Height, page.Size.Height - character.Y);
            int readableWidth = Math.Min(character.Width, pageWidth - character.X);

            for (int y = 0; y < character.Height; y++)
            {
                var pixelRowSpan = image.GetPixelRowSpan(y);
                int readOffset = y * pageWidth + character.X;

                for (int x = 0; x < character.Width; x++)
                    pixelRowSpan[x] = new Rgba32(255, 255, 255, x < readableWidth && y < readableHeight ? readBuffer[readOffset + x] : (byte)0);
            }

            return new TextureUpload(image);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (pageStreamHandles != null)
            {
                foreach (var h in pageStreamHandles)
                    h.Value?.Dispose();
            }
        }

        private byte[] readBuffer;

        private class PageInfo
        {
            public string Filename;
            public Size Size;
        }
    }
}
