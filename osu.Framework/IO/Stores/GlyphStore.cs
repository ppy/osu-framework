// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Cyotek.Drawing.BitmapFont;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<RawTexture>
    {
        private readonly string assetName;

        private readonly string fontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;

        private BitmapFont font;

        protected BitmapFont Font
        {
            get
            {
                try
                {
                    fontLoadTask?.Wait();
                }
                catch
                {
                    return null;
                }

                return font;
            }
        }

        private readonly TimedExpiryCache<int, RawTexture> texturePages = new TimedExpiryCache<int, RawTexture>();

        private Task fontLoadTask;

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null, bool precache = false)
        {
            this.store = store;
            this.assetName = assetName;

            fontName = assetName?.Split('/').Last();

            fontLoadTask = readFontMetadataAsync(precache);
        }

        private async Task readFontMetadataAsync(bool precache)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    font = new BitmapFont();
                    using (var s = store.GetStream($@"{assetName}.fnt"))
                        font.LoadText(s);

                    if (precache)
                        for (int i = 0; i < Font.Pages.Length; i++)
                            getTexturePage(i);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Couldn't load font asset from {assetName}.");
                    throw;
                }
            }, TaskCreationOptions.LongRunning);

            fontLoadTask = null;
        }

        public bool HasGlyph(char c) => Font.Characters.ContainsKey(c);
        public int GetBaseHeight() => Font.BaseHeight;

        public int? GetBaseHeight(string name)
        {
            if (name != fontName)
                return null;

            return Font.BaseHeight;
        }

        public RawTexture Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{fontName}/", StringComparison.Ordinal))
                return null;

            try
            {
                fontLoadTask?.Wait();
            }
            catch
            {
                return null;
            }

            if (!font.Characters.TryGetValue(name.Last(), out Character c))
                return null;

            RawTexture page = getTexturePage(c.TexturePage);
            loadedGlyphCount++;

            int width = c.Bounds.Width + c.Offset.X + 1;
            int height = c.Bounds.Height + c.Offset.Y + 1;
            int length = width * height * 4;
            byte[] pixels = new byte[length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int desti = y * width * 4 + x * 4;
                    if (x >= c.Offset.X && y >= c.Offset.Y
                        && x - c.Offset.X < c.Bounds.Width && y - c.Offset.Y < c.Bounds.Height)
                    {
                        int srci = (c.Bounds.Y + y - c.Offset.Y) * page.Width * 4
                                   + (c.Bounds.X + x - c.Offset.X) * 4;
                        pixels[desti] = page.Pixels[srci];
                        pixels[desti + 1] = page.Pixels[srci + 1];
                        pixels[desti + 2] = page.Pixels[srci + 2];
                        pixels[desti + 3] = page.Pixels[srci + 3];
                    }
                    else
                    {
                        pixels[desti] = 255;
                        pixels[desti + 1] = 255;
                        pixels[desti + 2] = 255;
                        pixels[desti + 3] = 0;
                    }
                }
            }

            return new RawTexture
            {
                Pixels = pixels,
                PixelFormat = OpenTK.Graphics.ES30.PixelFormat.Rgba,
                Width = width,
                Height = height,
            };
        }

        private RawTexture getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out RawTexture t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((font.Pages.Length - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = RawTexture.FromStream(stream));
            }

            return t;
        }

        public Stream GetStream(string name)
        {
            throw new NotSupportedException();
        }

        private int loadedPageCount;
        private int loadedGlyphCount;

        public override string ToString() => $@"GlyphStore({assetName}) LoadedPages:{loadedPageCount} LoadedGlyphs:{loadedGlyphCount}";

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                texturePages.Dispose();
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
