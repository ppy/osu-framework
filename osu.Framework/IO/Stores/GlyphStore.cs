// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Cyotek.Drawing.BitmapFont;
using osu.Framework.Allocation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<TextureUpload>
    {
        private readonly string assetName;

        public readonly string FontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;

        protected BitmapFont Font => completionSource.Task.Result;

        private readonly TimedExpiryCache<int, TextureUpload> texturePages = new TimedExpiryCache<int, TextureUpload>();

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

        private TextureUpload loadCharacter(Character c)
        {
            var page = getTexturePage(c.TexturePage);
            loadedGlyphCount++;

            int width = c.Bounds.Width + c.Offset.X + 1;
            int height = c.Bounds.Height + c.Offset.Y + 1;

            var image = new Image<Rgba32>(width, height);

            var pixels = image.GetPixelSpan();
            var span = page.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dest = y * width + x;

                    if (x >= c.Offset.X && y >= c.Offset.Y && x - c.Offset.X < c.Bounds.Width && y - c.Offset.Y < c.Bounds.Height)
                        pixels[dest] = span[(c.Bounds.Y + y - c.Offset.Y) * page.Width + (c.Bounds.X + x - c.Offset.X)];
                    else
                        pixels[dest] = new Rgba32(255, 255, 255, 0);
                }
            }

            return new TextureUpload(image);
        }

        private TextureUpload getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out TextureUpload t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((Font.Pages.Length - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = new TextureUpload(stream));
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
