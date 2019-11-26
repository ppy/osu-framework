// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Text;
using SharpFNT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A basic glyph store that will load font sprite sheets every character retrieval.
    /// </summary>
    /// <remarks>
    /// For better performing solutions, consider <see cref="RawCachingGlyphStore"/> or <see cref="TimedExpiryGlyphStore"/>
    /// </remarks>
    public class GlyphStore : IResourceStore<TextureUpload>, IGlyphStore
    {
        protected readonly string AssetName;

        public readonly string FontName;

        protected readonly ResourceStore<byte[]> Store;

        protected BitmapFont Font => completionSource.Task.Result;

        private readonly TaskCompletionSource<BitmapFont> completionSource = new TaskCompletionSource<BitmapFont>();

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            Store = new ResourceStore<byte[]>(store);

            Store.AddExtension("fnt");
            Store.AddExtension("bin");

            AssetName = assetName;

            FontName = assetName?.Split('/').Last();
        }

        private Task fontLoadTask;

        public Task LoadFontAsync() => fontLoadTask ??= Task.Factory.StartNew(() =>
        {
            try
            {
                BitmapFont font;
                using (var s = Store.GetStream($@"{AssetName}"))
                    font = BitmapFont.FromStream(s, FormatHint.Binary, false);

                completionSource.SetResult(font);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Couldn't load font asset from {AssetName}.");
                completionSource.SetResult(null);
                throw;
            }
        }, TaskCreationOptions.PreferFairness);

        public bool HasGlyph(char c) => Font.Characters.ContainsKey(c);

        public int GetBaseHeight() => Font.Common.Base;

        public int? GetBaseHeight(string name)
        {
            if (name != FontName)
                return null;

            return Font.Common.Base;
        }

        protected virtual Image<Rgba32> GetPageImageForCharacter(Character character)
        {
            using (var stream = Store.GetStream($@"{AssetName}_{character.Page.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png"))
                return TextureUpload.LoadFromStream<Rgba32>(stream);
        }

        public CharacterGlyph Get(char character)
        {
            var bmCharacter = Font.GetCharacter(character);
            return new CharacterGlyph(character, bmCharacter.XOffset, bmCharacter.YOffset, bmCharacter.XAdvance, this);
        }

        public int GetKerning(char left, char right) => Font.GetKerningAmount(left, right);

        Task<CharacterGlyph> IResourceStore<CharacterGlyph>.GetAsync(string name) => Task.Run(() => ((IGlyphStore)this).Get(name[0]));

        CharacterGlyph IResourceStore<CharacterGlyph>.Get(string name) => Get(name[0]);

        public TextureUpload Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!Font.Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return LoadCharacter(c);
        }

        public virtual async Task<TextureUpload> GetAsync(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!(await completionSource.Task).Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return LoadCharacter(c);
        }

        protected int LoadedGlyphCount;

        protected virtual TextureUpload LoadCharacter(Character character)
        {
            var page = GetPageImageForCharacter(character);
            LoadedGlyphCount++;

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, character.Width, character.Height, new Rgba32(255, 255, 255, 0));

            var dest = image.GetPixelSpan();
            var source = page.GetPixelSpan();

            // the spritesheet may have unused pixels trimmed
            int readableHeight = Math.Min(character.Height, page.Height - character.Y);
            int readableWidth = Math.Min(character.Width, page.Width - character.X);

            for (int y = 0; y < readableHeight; y++)
            {
                int readOffset = (character.Y + y) * page.Width + character.X;
                int writeOffset = y * character.Width;

                for (int x = 0; x < readableWidth; x++)
                    dest[writeOffset + x] = source[readOffset + x];
            }

            return new TextureUpload(image);
        }

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
