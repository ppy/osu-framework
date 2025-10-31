// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    public class OutlineFontStore : FontStore
    {
        private OutlineFont font;

        public OutlineFontStore(IRenderer renderer, IResourceStore<byte[]> store, string assetName, float scaleAdjust = 100)
            : base(renderer, null, scaleAdjust)
        {
            font = new OutlineFont(store, assetName, 0)
            {
                Resolution = (uint)Math.Round(scaleAdjust)
            };
        }

        protected override void Dispose(bool disposing)
        {
            font.Dispose();
        }

        public void AddInstance(FontVariation? variation)
        {
            AddTextureSource(new OutlineGlyphStore(font, variation));
        }

        public void AddInstance(string namedInstance, string? nameOverride = null)
        {
            AddTextureSource(new OutlineGlyphStore(font, namedInstance, nameOverride));
        }
    }
}
