// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using SharpFNT;
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

        /// <summary>
        /// Gets the spacing information for the specified character
        /// </summary>
        /// <param name="c">The character to retrieve spacing information for</param>
        /// <returns>The spacing information for the specified character, or null if the character is not found</returns>
        public CharacterGlyph? GetGlyphInfo(char c)
        {
            Character character = Font.GetCharacter(c);
            if (character != null)
            {
                return new CharacterGlyph
                {
                    XOffset = character.XOffset,
                    YOffset = character.YOffset,
                    XAdvance = character.XAdvance
                };
            }

            return null;
        }

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

        private TextureUpload loadCharacter(Character c)
        {
            var page = getTexturePage(c.Page);
            loadedGlyphCount++;

            int width = c.Width;
            int height = c.Height;

            var image = new Image<Rgba32>(width, height);

            var pixels = image.GetPixelSpan();
            var span = page.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dest = y * width + x;
                    pixels[dest] = span[(c.Y + y) * page.Width + c.X + x];
                }
            }

            return new TextureUpload(image);
        }

        private TextureUpload getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out TextureUpload t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = new TextureUpload(stream));
            }

            return t;
        }

        public Stream GetStream(string name) => throw new NotSupportedException();

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
