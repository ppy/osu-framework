// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A self-contained glyph store that rasterizes glyphs from outlines.
    /// </summary>
    public class SelfContainedOutlineGlyphStore : OutlineGlyphStore
    {
        public SelfContainedOutlineGlyphStore(IResourceStore<byte[]> store, string assetName, string? nameOverride = null)
            : base(new OutlineFont(store, assetName, 0) { Resolution = 100 }, (FontVariation?)null, nameOverride)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            Font.Dispose();
        }
    }
}

