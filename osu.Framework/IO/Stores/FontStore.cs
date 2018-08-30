// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;

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
            {
                glyphStores.Add(gs);
                queueLoad(gs);
            }

            base.AddStore(store);
        }

        private Task childStoreLoadTasks;

        /// <summary>
        /// Append child stores to a single threaded load task.
        /// </summary>
        private void queueLoad(GlyphStore store)
        {
            var previousLoadStream = childStoreLoadTasks;

            childStoreLoadTasks = Task.Run(async () =>
            {
                if (previousLoadStream != null)
                    await previousLoadStream;

                try
                {
                    Logger.Log($"Loading Font {store.FontName}...", LoggingTarget.Debug);
                    await store.LoadFontAsync();
                    Logger.Log($"Loaded Font {store.FontName}!", LoggingTarget.Debug);
                }
                catch (OperationCanceledException)
                {
                }
            });
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
