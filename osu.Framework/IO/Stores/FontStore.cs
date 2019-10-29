// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;
using osu.Framework.Platform;
using osu.Framework.Text;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.IO.Stores
{
    public class FontStore : TextureStore, ITexturedGlyphLookupStore
    {
        private readonly List<GlyphStore> glyphStores = new List<GlyphStore>();

        private readonly List<FontStore> nestedFontStores = new List<FontStore>();

        private Storage cacheStorage;

        /// <summary>
        /// A local cache to avoid string allocation overhead. Can be changed to (string,char)=>string if this ever becomes an issue,
        /// but as long as we directly inherit <see cref="TextureStore"/> this is a slight optimisation.
        /// </summary>
        private readonly ConcurrentDictionary<(string, char), ITexturedCharacterGlyph> namespacedGlyphCache = new ConcurrentDictionary<(string, char), ITexturedCharacterGlyph>();

        public FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100)
            : this(store, scaleAdjust, false)
        {
        }

        internal FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100, bool useAtlas = false, Storage cacheStorage = null)
            : base(store, scaleAdjust: scaleAdjust, useAtlas: useAtlas)
        {
            this.cacheStorage = cacheStorage;
        }

        protected override IEnumerable<string> GetFilenames(string name) =>
            // extensions should not be used as they interfere with character lookup.
            name.Yield();

        public override void AddStore(IResourceStore<TextureUpload> store)
        {
            switch (store)
            {
                case FontStore fs:
                    if (fs.Atlas == null)
                    {
                        // share the main store's atlas.
                        fs.Atlas = Atlas;
                    }

                    if (fs.cacheStorage == null)
                        fs.cacheStorage = cacheStorage;

                    nestedFontStores.Add(fs);
                    return;

                case GlyphStore gs:

                    if (gs.CacheStorage == null)
                        gs.CacheStorage = cacheStorage;

                    glyphStores.Add(gs);
                    queueLoad(gs);
                    break;
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
                    Logger.Log($"Loading Font {store.FontName}...", level: LogLevel.Debug);
                    await store.LoadFontAsync();
                    Logger.Log($"Loaded Font {store.FontName}!", level: LogLevel.Debug);
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        public override void RemoveStore(IResourceStore<TextureUpload> store)
        {
            switch (store)
            {
                case FontStore fs:
                    nestedFontStores.Remove(fs);
                    return;

                case GlyphStore gs:
                    glyphStores.Remove(gs);
                    break;
            }

            base.RemoveStore(store);
        }

        public override Texture Get(string name)
        {
            var found = base.Get(name);

            if (found == null)
            {
                foreach (var store in nestedFontStores)
                    if ((found = store.Get(name)) != null)
                        break;
            }

            return found;
        }

        public ITexturedCharacterGlyph Get(string fontName, char character)
        {
            var key = (fontName, character);

            if (namespacedGlyphCache.TryGetValue(key, out var existing))
                return existing;

            string textureName = string.IsNullOrEmpty(fontName) ? character.ToString() : $"{fontName}/{character}";

            foreach (var store in glyphStores)
            {
                if ((string.IsNullOrEmpty(fontName) || fontName == store.FontName) && store.HasGlyph(character))
                    return namespacedGlyphCache[key] = new TexturedCharacterGlyph(store.Get(character), Get(textureName), 1 / ScaleAdjust);
            }

            foreach (var store in nestedFontStores)
            {
                var glyph = store.Get(fontName, character);
                if (glyph != null)
                    return namespacedGlyphCache[key] = glyph;
            }

            return namespacedGlyphCache[key] = null;
        }

        public Task<ITexturedCharacterGlyph> GetAsync(string fontName, char character) => Task.Run(() => Get(fontName, character));

        public float? GetBaseHeight(char c)
        {
            foreach (var store in glyphStores)
            {
                if (store.HasGlyph(c))
                    return store.GetBaseHeight() / ScaleAdjust;
            }

            foreach (var store in nestedFontStores)
            {
                var height = store.GetBaseHeight(c);
                if (height.HasValue)
                    return height;
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

            foreach (var store in nestedFontStores)
            {
                var height = store.GetBaseHeight(fontName);
                if (height.HasValue)
                    return height;
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
