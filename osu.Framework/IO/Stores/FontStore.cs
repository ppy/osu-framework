// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;
using System.Collections.Generic;

namespace osu.Framework.IO.Stores
{
    public class FontStore : TextureStore
    {
        private readonly List<GlyphStore> glyphStores = new List<GlyphStore>();

        public FontStore()
        {
        }

        public FontStore(GlyphStore glyphStore)
            : base(glyphStore)
        {
        }

        public override void AddStore(IResourceStore<RawTexture> store)
        {
            if (store is GlyphStore gs)
                glyphStores.Add(gs);
            base.AddStore(store);
        }

        public override void RemoveStore(IResourceStore<RawTexture> store)
        {
            if (store is GlyphStore gs)
                glyphStores.Remove(gs);
            base.RemoveStore(store);
        }

        public float? GetBaseHeight(char c)
        {
            foreach (var store in glyphStores)
            {
                if (store.HasGlyph(c))
                    return store.GetBaseHeight() / ScaleAdjust;
            }

            return null;
        }

        public float? GetBaseHeight(string fontName)
        {
            foreach (var store in glyphStores)
            {
                var bh = store.GetBaseHeight(fontName);
                if (bh.HasValue)
                    return bh.Value / ScaleAdjust;
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            glyphStores.ForEach(g => g.Dispose());
        }
    }
}
