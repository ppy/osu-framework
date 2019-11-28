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
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A glyph store which caches font sprite sheets as raw pixels to disk on first use.
    /// </summary>
    /// <remarks>
    /// This results in memory efficient lookups with good performance on solid state backed devices.
    /// </remarks>
    public class RawCachingGlyphStore : GlyphStore
    {
        public Storage CacheStorage;

        public RawCachingGlyphStore(ResourceStore<byte[]> store, string assetName = null)
            : base(store, assetName)
        {
        }

        private readonly Dictionary<int, PageInfo> pageLookup = new Dictionary<int, PageInfo>();

        protected override TextureUpload LoadCharacter(Character character)
        {
            if (!pageLookup.TryGetValue(character.Page, out var pageInfo))
                pageInfo = createCachedPageInfo(character.Page);

            return createTextureUpload(character, pageInfo);
        }

        private PageInfo createCachedPageInfo(int page)
        {
            string filename = GetFilenameForPage(page);

            using (var stream = Store.GetStream(filename))
            {
                string streamMd5 = stream.ComputeMD5Hash();
                string filenameMd5 = filename.ComputeMD5Hash();

                string accessFilename = $"{filenameMd5}#{streamMd5}";

                var existing = CacheStorage.GetFiles(string.Empty, $"{accessFilename}*").FirstOrDefault();

                if (existing != null)
                {
                    var split = existing.Split('#');
                    return pageLookup[page] = new PageInfo
                    {
                        Size = new Size(int.Parse(split[2]), int.Parse(split[3])),
                        Filename = existing
                    };
                }

                using (var convert = GetPageImage(page))
                {
                    // todo: use i# memoryallocator once netstandard supports stream operations
                    byte[] output = new byte[convert.Width * convert.Height];

                    var pxl = convert.GetPixelSpan();

                    for (int i = 0; i < convert.Width * convert.Height; i++)
                        output[i] = pxl[i].A;

                    // ensure any stale cached versions are deleted.
                    foreach (var f in CacheStorage.GetFiles(string.Empty, $"{filenameMd5}*"))
                        CacheStorage.Delete(f);

                    accessFilename += $"#{convert.Width}#{convert.Height}";

                    using (var outStream = CacheStorage.GetStream(accessFilename, FileAccess.Write, FileMode.Create))
                        outStream.Write(output, 0, output.Length);

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
            int pageWidth = page.Size.Width;

            if (readBuffer == null || readBuffer.Length < pageWidth)
                readBuffer = new byte[pageWidth];

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, character.Width, character.Height);

            using (var source = CacheStorage.GetStream(page.Filename))
            {
                var dest = image.GetPixelSpan();
                source.Seek(pageWidth * character.Y, SeekOrigin.Current);

                // the spritesheet may have unused pixels trimmed
                int readableHeight = Math.Min(character.Height, page.Size.Height - character.Y);
                int readableWidth = Math.Min(character.Width, pageWidth - character.X);

                for (int y = 0; y < character.Height; y++)
                {
                    source.Read(readBuffer, 0, pageWidth);

                    int writeOffset = y * character.Width;

                    for (int x = 0; x < character.Width; x++)
                        dest[writeOffset + x] = new Rgba32(255, 255, 255, x < readableWidth && y < readableHeight ? readBuffer[character.X + x] : (byte)0);
                }
            }

            return new TextureUpload(image);
        }

        private byte[] readBuffer;

        private class PageInfo
        {
            public string Filename;
            public Size Size;
        }
    }
}
