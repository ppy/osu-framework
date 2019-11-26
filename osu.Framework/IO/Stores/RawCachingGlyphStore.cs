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
    public class RawCachingGlyphStore : GlyphStore
    {
        public Storage CacheStorage;

        public RawCachingGlyphStore(ResourceStore<byte[]> store, string assetName = null)
            : base(store, assetName)
        {
        }

        private readonly Dictionary<int, PageInfo> pageLookup = new Dictionary<int, PageInfo>();

        protected override TextureUpload LoadCharacter(Character c)
        {
            if (!pageLookup.TryGetValue(c.Page, out var pageInfo))
            {
                string filename = $@"{AssetName}_{c.Page.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png";

                using (var stream = Store.GetStream(filename))
                {
                    string streamMd5 = stream.ComputeMD5Hash();
                    string filenameMd5 = filename.ComputeMD5Hash();

                    string accessFilename = $"{filenameMd5}#{streamMd5}";

                    var existing = CacheStorage.GetFiles(string.Empty, $"{accessFilename}*").FirstOrDefault();

                    if (existing != null)
                    {
                        var split = existing.Split('#');
                        pageLookup[c.Page] = pageInfo = new PageInfo
                        {
                            Size = new Size(int.Parse(split[2]), int.Parse(split[3])),
                            Filename = existing
                        };
                    }
                    else
                    {
                        using (var convert = GetPageImageForCharacter(c))
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

                            pageLookup[c.Page] = pageInfo = new PageInfo
                            {
                                Size = new Size(convert.Width, convert.Height),
                                Filename = accessFilename
                            };
                        }
                    }
                }
            }

            return createTextureUpload(c, pageInfo);
        }

        private TextureUpload createTextureUpload(Character character, PageInfo page)
        {
            int pageWidth = page.Size.Width;

            int charWidth = character.Width;

            int charHeight = character.Height;
            if (readBuffer == null || readBuffer.Length < pageWidth)
                readBuffer = new byte[pageWidth];

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, charWidth, charHeight, new Rgba32(255, 255, 255, 0));

            using (var stream = CacheStorage.GetStream(page.Filename))
            {
                var pixels = image.GetPixelSpan();
                stream.Seek(pageWidth * character.Y, SeekOrigin.Current);

                // the spritesheet may have unused pixels trimmed
                int readableHeight = Math.Min(character.Height, page.Size.Height - character.Y);
                int readableWidth = Math.Min(character.Width, pageWidth - character.X);

                for (int y = 0; y < readableHeight; y++)
                {
                    stream.Read(readBuffer, 0, pageWidth);

                    int writeOffset = y * charWidth;

                    for (int x = 0; x < readableWidth; x++)
                        pixels[writeOffset + x] = new Rgba32(255, 255, 255, readBuffer[character.X + x]);
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
