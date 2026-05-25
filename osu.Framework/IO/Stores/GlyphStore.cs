// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using JetBrains.Annotations;
using osu.Framework.Extensions;
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
    public class GlyphStore : IResourceStore<TextureUpload>, IGlyphStore
    {
        protected readonly string AssetName;

        protected readonly IResourceStore<TextureUpload> TextureLoader;

        public string FontName { get; }

        public float? Baseline => Font?.Common.Base;

        protected readonly ResourceStore<byte[]> Store;

        [CanBeNull]
        protected BitmapFont Font => completionSource.Task.GetResultSafely();

        private readonly TaskCompletionSource<BitmapFont> completionSource = new TaskCompletionSource<BitmapFont>();

        /// <summary>
        /// This is a rare usage of a static framework-wide cache.
        /// In normal execution font instances are held locally by font stores and this will add no overhead or improvement.
        /// It exists specifically to avoid overheads of parsing fonts repeatedly in unit tests.
        /// </summary>
        private static readonly ConcurrentDictionary<string, BitmapFont> font_cache = new ConcurrentDictionary<string, BitmapFont>();

        /// <summary>
        /// Create a new glyph store.
        /// </summary>
        /// <param name="store">The store to provide font resources.</param>
        /// <param name="assetName">The base name of the font.</param>
        /// <param name="textureLoader">An optional platform-specific store for loading textures. Should load for the store provided in <param ref="param"/>.</param>
        public GlyphStore(ResourceStore<byte[]> store, string assetName = null, IResourceStore<TextureUpload> textureLoader = null)
        {
            Store = new ResourceStore<byte[]>(store);

            Store.AddExtension("fnt");
            Store.AddExtension("bin");

            AssetName = assetName;
            TextureLoader = textureLoader;

            FontName = assetName?.Split('/').Last() ?? string.Empty;
        }

        private Task fontLoadTask;

        public Task LoadFontAsync() => fontLoadTask ??= Task.Factory.StartNew(() =>
        {
            try
            {
                BitmapFont font;

                using (var s = Store.GetStream($@"{AssetName}"))
                {
                    string hash = s.ComputeMD5Hash();

                    if (font_cache.TryGetValue(hash, out font))
                    {
                        Logger.Log($"Cached font load for {AssetName}");
                    }
                    else
                    {
                        font_cache.TryAdd(hash, font = BitmapFont.FromStream(s, FormatHint.Binary, false));
                    }
                }

                completionSource.SetResult(font);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Couldn't load font asset from {AssetName}.");
                completionSource.SetResult(null);
                throw;
            }
        }, TaskCreationOptions.PreferFairness);

        public bool HasGlyph(char c) => HasGlyph((int)c);
        public bool HasGlyph(int codepoint) => Font?.Characters.ContainsKey(codepoint) == true;

        protected virtual TextureUpload GetPageImage(int page)
        {
            if (TextureLoader != null)
                return TextureLoader.Get(GetFilenameForPage(page));

            using (var stream = Store.GetStream(GetFilenameForPage(page)))
                return new TextureUpload(stream);
        }

        protected string GetFilenameForPage(int page)
        {
            Debug.Assert(Font != null);
            return $@"{AssetName}_{page.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png";
        }

        public CharacterGlyph Get(char character) => Get((int)character);

        public CharacterGlyph Get(int codepoint)
        {
            if (Font == null)
                return null;

            Debug.Assert(Baseline != null);

            if (!Rune.IsValid(codepoint))
                return null;

            Font.Characters.TryGetValue(codepoint, out Character bmCharacter);

            return bmCharacter == null
                ? null
                : new CharacterGlyph(codepoint, bmCharacter.XOffset, bmCharacter.YOffset, bmCharacter.XAdvance, Baseline.Value, this);
        }

        public int GetKerning(char left, char right)
        {
            int leftCodepoint = left;
            int rightCodepoint = right;
            return GetKerning(leftCodepoint, rightCodepoint);
        }

        public int GetKerning(int leftCodepoint, int rightCodepoint)
        {
            if (leftCodepoint > char.MaxValue || rightCodepoint > char.MaxValue)
                return 0;

            return Font?.GetKerningAmount((char)leftCodepoint, (char)rightCodepoint) ?? 0;
        }

        Task<CharacterGlyph> IResourceStore<CharacterGlyph>.GetAsync(string name, CancellationToken cancellationToken) =>
            Task.Run(() => ((IGlyphStore)this).Get(name[0]), cancellationToken);

        CharacterGlyph IResourceStore<CharacterGlyph>.Get(string name) => Get(name[0]);

        public TextureUpload Get(string name)
        {
            if (Font == null) return null;

            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            if (!tryParseCodepointFromResourceName(name, out int codepoint))
                return null;

            return Font.Characters.TryGetValue(codepoint, out Character c) ? LoadCharacter(c) : null;
        }

        public virtual async Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null;

            var bmFont = await completionSource.Task.ConfigureAwait(false);

            if (!tryParseCodepointFromResourceName(name, out int codepoint))
                return null;

            return bmFont.Characters.TryGetValue(codepoint, out Character c)
                ? LoadCharacter(c)
                : null;
        }

        private static bool tryParseCodepointFromResourceName(string name, out int codepoint)
        {
            codepoint = 0;

            if (string.IsNullOrEmpty(name))
                return false;

            int slashIndex = name.LastIndexOf('/');
            string suffix = slashIndex >= 0 ? name[(slashIndex + 1)..] : name;

            if (suffix.Length == 0)
                return false;

            if (suffix.Length == 1)
            {
                codepoint = suffix[0];
                return true;
            }

            return int.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codepoint)
                   && Rune.IsValid(codepoint);
        }

        protected int LoadedGlyphCount;

        protected virtual TextureUpload LoadCharacter(Character character)
        {
            var page = GetPageImage(character.Page);
            LoadedGlyphCount++;

            var image = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, character.Width, character.Height);
            var source = page.Data;

            // the spritesheet may have unused pixels trimmed
            int readableHeight = Math.Min(character.Height, page.Height - character.Y);
            int readableWidth = Math.Min(character.Width, page.Width - character.X);

            for (int y = 0; y < character.Height; y++)
            {
                var pixelRowMemory = image.DangerousGetPixelRowMemory(y);
                int readOffset = (character.Y + y) * page.Width + character.X;

                for (int x = 0; x < character.Width; x++)
                    pixelRowMemory.Span[x] = x < readableWidth && y < readableHeight ? source[readOffset + x] : new Rgba32(255, 255, 255, 0);
            }

            return new TextureUpload(image);
        }

        public Stream GetStream(string name) => throw new NotSupportedException();

        public IEnumerable<string> GetAvailableResources() => Font?.Characters.Keys.Select(k => k <= char.MaxValue ? $"{FontName}/{(char)k}" : $"{FontName}/{k:x}") ?? Enumerable.Empty<string>();

        #region IDisposable Support

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion
    }
}
