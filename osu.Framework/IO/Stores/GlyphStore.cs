﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using SharpFNT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<TextureUpload>
    {
        private readonly string assetName;

        public readonly string FontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;

        protected BitmapFont Font => completionSource.Task.Result;

        private readonly TaskCompletionSource<BitmapFont> completionSource = new TaskCompletionSource<BitmapFont>();

        internal Storage CacheStorage;

        private Task fontLoadTask;

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            this.store = new ResourceStore<byte[]>(store);

            this.store.AddExtension("fnt");
            this.store.AddExtension("bin");

            this.assetName = assetName;

            FontName = assetName?.Split('/').Last();
        }

        public Task LoadFontAsync() => fontLoadTask ?? (fontLoadTask = Task.Factory.StartNew(() =>
        {
            try
            {
                BitmapFont font;
                using (var s = store.GetStream($@"{assetName}"))
                    font = BitmapFont.FromStream(s, FormatHint.Binary, false);

                completionSource.SetResult(font);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Couldn't load font asset from {assetName}.");
                completionSource.SetResult(null);
                throw;
            }
        }, TaskCreationOptions.PreferFairness));

        public bool HasGlyph(char c) => Font.Characters.ContainsKey(c);

        public int GetBaseHeight() => Font.Common.Base;

        public int? GetBaseHeight(string name)
        {
            if (name != FontName)
                return null;

            return Font.Common.Base;
        }

        public TextureUpload Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!Font.Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        public virtual async Task<TextureUpload> GetAsync(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!(await completionSource.Task).Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        private readonly Dictionary<int, PageInfo> pageLookup = new Dictionary<int, PageInfo>();

        private class PageInfo
        {
            public string Filename;
            public Size Size;
        }

        private TextureUpload loadCharacter(Character c)
        {
            if (!pageLookup.TryGetValue(c.Page, out var pageInfo))
            {
                string filename = $@"{assetName}_{c.Page.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png";

                using (var stream = store.GetStream(filename))
                using (var convert = Image.Load(stream))
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

            int pageWidth = pageInfo.Size.Width;

            int charWidth = c.Width + c.XOffset;
            int charHeight = c.Height + c.YOffset;

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, charWidth, charHeight, new Rgba32(255, 255, 255, 0));

            using (var stream = CacheStorage.GetStream(pageInfo.Filename))
            {
                var pixels = image.GetPixelSpan();
                stream.Seek(pageWidth * c.Y, SeekOrigin.Current);

                for (int y = 0; y < c.Height; y++)
                {
                    stream.Read(readBuffer, 0, pageWidth);

                    for (int x = 0; x < c.Width; x++)
                    {
                        int offsetX = x + c.XOffset;
                        int offsetY = y + c.YOffset;

                        if (offsetX >= 0 && offsetY > 0 && offsetX < charWidth && offsetY < charHeight) // some glyphs can be offset beyond the valid texture bounds; ignore these pixels.
                            pixels[offsetY * charWidth + offsetX] = new Rgba32(255, 255, 255, readBuffer[c.X + x]);
                    }
                }
            }

            return new TextureUpload(image);
        }

        private readonly byte[] readBuffer = new byte[1024];

        public Stream GetStream(string name) => throw new NotSupportedException();

        public IEnumerable<string> GetAvailableResources() => Font.Characters.Keys.Select(k => $"{FontName}/{(char)k}");

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~GlyphStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
