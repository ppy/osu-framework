// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A glyph store that rasterizes glyphs from outlines.
    /// </summary>
    public class OutlineGlyphStore : IGlyphStore, IResourceStore<TextureUpload>
    {
        protected OutlineFont Font { get; }

        private RawFontVariation? rawVariation;

        public FontVariation? Variation { get; }

        public string FontName { get; }

        public float? Baseline => Font.Baseline;

        private readonly bool selfContained;

        /// <summary>
        /// Create a glyph store for a font using the specified OpenType named instance.
        /// </summary>
        /// <param name="font">The underlying font.</param>
        /// <param name="namedInstance">The named instance to select.</param>
        /// <param name="nameOverride">
        /// The value of <see cref="FontName"/>. If null, <paramref name="namedInstance"/> will be used.
        /// </param>
        public OutlineGlyphStore(OutlineFont font, string namedInstance, string? nameOverride = null)
            : this(font, new FontVariation { NamedInstance = namedInstance }, nameOverride)
        {
        }

        /// <summary>
        /// Create a glyph store for a font using the specified OpenType variation parameters.
        /// </summary>
        /// <param name="font">The underlying font.</param>
        /// <param name="variation">The font variation parameters.</param>
        /// <param name="nameOverride">
        /// The value of <see cref="FontName"/>. If null, it will be computed using a naming scheme based on
        /// <see href="https://download.macromedia.com/pub/developer/opentype/tech-notes/5902.AdobePSNameGeneration.html"/>.
        /// </param>
        public OutlineGlyphStore(OutlineFont font, FontVariation? variation = null, string? nameOverride = null)
        {
            Font = font;
            Variation = variation;

            FontName = nameOverride ?? variation?.GenerateInstanceName(font.AssetName) ?? font.AssetName;
        }

        /// <summary>
        /// Load a new font and create a glyph store for it.
        /// </summary>
        /// <param name="store">The font's resource store.</param>
        /// <param name="assetName">The asset name of the font.</param>
        public OutlineGlyphStore(IResourceStore<byte[]> store, string assetName)
            : this(new OutlineFont(store, assetName, 0) { Resolution = 100 }, (FontVariation?)null, assetName)
        {
            selfContained = true;
        }

        ~OutlineGlyphStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (selfContained)
                Font.Dispose();
        }

        public async Task LoadFontAsync()
        {
            try
            {
                await Font.LoadAsync().ConfigureAwait(false);
                rawVariation = Font.DecodeFontVariation(Variation);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Couldn't load font {FontName} from {Font.AssetPath}.");
                throw;
            }
        }

        public bool HasGlyph(char c)
        {
            return Font.HasGlyph(c);
        }

        public CharacterGlyph? Get(char c)
        {
            var metrics = Font.GetMetrics(Font.GetGlyphIndex(c), rawVariation);

            if (metrics is null)
                return null;

            return new CharacterGlyph(c, metrics.XOffset, metrics.YOffset, metrics.XAdvance, metrics.Baseline, this);
        }

        public int GetKerning(char left, char right)
        {
            return Font.GetKerning(Font.GetGlyphIndex(left), Font.GetGlyphIndex(right), rawVariation);
        }

        Task<CharacterGlyph> IResourceStore<CharacterGlyph>.GetAsync(string name, CancellationToken cancellationToken)
            => Task.Run(() => ((IGlyphStore)this).Get(name[0]), cancellationToken)!;

        CharacterGlyph IResourceStore<CharacterGlyph>.Get(string name) => Get(name[0])!;

        public TextureUpload Get(string name)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null!;

            char c = name.Last();
            uint glyphIndex = Font.GetGlyphIndex(c);

            return Font.RasterizeGlyph(glyphIndex, rawVariation)!;
        }

        public async Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name.Length > 1 && !name.StartsWith($@"{FontName}/", StringComparison.Ordinal))
                return null!;

            char c = name.Last();
            uint glyphIndex = await Font.GetGlyphIndexAsync(c).ConfigureAwait(false);

            return await Font.RasterizeGlyphAsync(glyphIndex, rawVariation, cancellationToken).ConfigureAwait(false);
        }

        public Stream GetStream(string name) => throw new NotSupportedException();

        public IEnumerable<string> GetAvailableResources()
        {
            return Font.GetAvailableChars().Select(c => $@"{FontName}/{c}");
        }
    }
}

