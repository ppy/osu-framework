// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A glyph store which caches font sprite sheets in memory temporary, to allow for more efficient retrieval.
    /// </summary>
    /// <remarks>
    /// This store has a higher memory overhead than <see cref="RawCachingGlyphStore"/>, but better performance and zero disk footprint.
    /// </remarks>
    public class TimedExpiryGlyphStore : GlyphStore
    {
        private readonly TimedExpiryCache<int, Image<Rgba32>> texturePages = new TimedExpiryCache<int, Image<Rgba32>>();

        public TimedExpiryGlyphStore(ResourceStore<byte[]> store, string assetName = null)
            : base(store, assetName)
        {
        }

        protected override Image<Rgba32> GetPageImage(int page)
        {
            if (!texturePages.TryGetValue(page, out var image))
            {
                loadedPageCount++;
                texturePages.Add(page, image = base.GetPageImage(page));
            }

            return image;
        }

        private int loadedPageCount;

        public override string ToString() => $@"GlyphStore({AssetName}) LoadedPages:{loadedPageCount} LoadedGlyphs:{LoadedGlyphCount}";

        protected override void Dispose(bool disposing)
        {
            texturePages.Dispose();
        }
    }
}
