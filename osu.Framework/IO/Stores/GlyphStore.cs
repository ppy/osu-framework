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

        public readonly string FontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;

        protected BitmapFont Font => completionSource.Task.Result;

        private readonly TimedExpiryCache<int, RawTexture> texturePages = new TimedExpiryCache<int, RawTexture>();

        private readonly TaskCompletionSource<BitmapFont> completionSource = new TaskCompletionSource<BitmapFont>();

        private Task fontLoadTask;

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            this.store = store;
            this.assetName = assetName;

            FontName = assetName?.Split('/').Last();
        }

        public Task LoadFontAsync() => fontLoadTask ?? (fontLoadTask = Task.Factory.StartNew(() =>
        {
            try
            {
                var font = new BitmapFont();
                using (var s = store.GetStream($@"{assetName}.fnt"))
                    font.LoadText(s);

                completionSource.SetResult(font);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Couldn't load font asset from {assetName}.");
                throw;
            }
        }, TaskCreationOptions.PreferFairness));

        public bool HasGlyph(char c) => Font.Characters.ContainsKey(c);
        public int GetBaseHeight() => Font.BaseHeight;

        public int? GetBaseHeight(string name)
        {
            if (name != FontName)
                return null;

            return Font.BaseHeight;
        }

        public RawTexture Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!Font.Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        public virtual async Task<RawTexture> GetAsync(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!(await completionSource.Task).Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        private RawTexture loadCharacter(Character c)
        {
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
                        pixels[desti] = page.Data[srci];
                        pixels[desti + 1] = page.Data[srci + 1];
                        pixels[desti + 2] = page.Data[srci + 2];
                        pixels[desti + 3] = page.Data[srci + 3];
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

            return new RawTexture(width, height, pixels);
        }

        private RawTexture getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out RawTexture t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((Font.Pages.Length - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = new RawTexture(stream));
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
