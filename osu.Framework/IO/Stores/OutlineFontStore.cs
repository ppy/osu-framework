// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Text;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A resource store for instantiating multiple variations of a single font.
    /// </summary>
    public class OutlineFontStore : FontStore
    {
        private readonly OutlineFont font;

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

        /// <summary>
        /// Instantiate a font variation and add it to the store.
        /// </summary>
        /// <param name="variation">The parameters of the font.</param>
        /// <param name="nameOverride">If not null, overrides the name for the new instance.</param>
        public void AddInstance(FontVariation? variation, string? nameOverride = null)
        {
            AddTextureSource(new OutlineGlyphStore(font, variation, nameOverride));
        }

        /// <summary>
        /// Instantiate a font variation and add it to the store.
        /// </summary>
        /// <param name="namedInstance">A named instance of the font.</param>
        /// <param name="nameOverride">If not null, overrides the name for the new instance.</param>
        public void AddInstance(string namedInstance, string? nameOverride = null)
        {
            AddTextureSource(new OutlineGlyphStore(font, namedInstance, nameOverride));
        }
    }
}
