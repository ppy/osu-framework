// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using osu.Framework.Platform;
using osu.Framework.Text;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.OpenGL.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.IO.Stores
{
    public class FontStore : TextureStore, ITexturedGlyphLookupStore
    {
        private readonly List<IGlyphStore> glyphStores = new List<IGlyphStore>();

        private readonly List<FontStore> nestedFontStores = new List<FontStore>();

        private Storage cacheStorage;

        /// <summary>
        /// A local cache to avoid string allocation overhead. Can be changed to (string,char)=>string if this ever becomes an issue,
        /// but as long as we directly inherit <see cref="TextureStore"/> this is a slight optimisation.
        /// </summary>
        private readonly ConcurrentDictionary<(string, char), ITexturedCharacterGlyph> namespacedGlyphCache = new ConcurrentDictionary<(string, char), ITexturedCharacterGlyph>();

        /// <summary>
        /// Construct a font store to be added to a parent font store via <see cref="AddStore"/>.
        /// </summary>
        /// <param name="store">The texture source.</param>
        /// <param name="scaleAdjust">The raw pixel height of the font. Can be used to apply a global scale or metric to font usages.</param>
        public FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100)
            : this(store, scaleAdjust, false)
        {
        }

        /// <summary>
        /// Construct a font store with a custom filtering mode to be added to a parent font store via <see cref="AddStore"/>.
        /// All fonts that use the specified filter mode should be nested inside this store to make optimal use of texture atlases.
        /// </summary>
        /// <param name="store">The texture source.</param>
        /// <param name="scaleAdjust">The raw pixel height of the font. Can be used to apply a global scale or metric to font usages.</param>
        /// <param name="minFilterMode">The texture minification filtering mode to use.</param>
        public FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100, All minFilterMode = All.Linear)
            : this(store, scaleAdjust, true, filteringMode: minFilterMode)
        {
        }

        internal FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100, bool useAtlas = false, Storage cacheStorage = null, All filteringMode = All.Linear)
            : base(store, scaleAdjust: scaleAdjust, useAtlas: useAtlas, filteringMode: filteringMode)
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
                    // if null, share the main store's atlas.
                    fs.Atlas ??= Atlas;
                    fs.cacheStorage ??= cacheStorage;

                    nestedFontStores.Add(fs);
                    return;

                case IGlyphStore gs:

                    if (gs is RawCachingGlyphStore raw && raw.CacheStorage == null)
                        raw.CacheStorage = cacheStorage;

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
        private void queueLoad(IGlyphStore store)
        {
            var previousLoadStream = childStoreLoadTasks;

            childStoreLoadTasks = Task.Run(async () =>
            {
                if (previousLoadStream != null)
                    await previousLoadStream.ConfigureAwait(false);

                try
                {
                    Logger.Log($"Loading Font {store.FontName}...", level: LogLevel.Debug);
                    await store.LoadFontAsync().ConfigureAwait(false);
                    Logger.Log($"Loaded Font {store.FontName}!", level: LogLevel.Debug);
                }
                catch
                {
                    // Errors are logged by LoadFontAsync() but also propagated outwards.
                    // We can gracefully continue when loading a font fails, so the exception shouldn't trigger the unobserved exception handler of GameHost and potentially crash the game.
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

        public new Texture Get(string name)
        {
            var found = base.Get(name, WrapMode.None, WrapMode.None);

            if (found == null)
            {
                foreach (var store in nestedFontStores)
                {
                    if ((found = store.Get(name)) != null)
                        break;
                }
            }

            return found;
        }

        [CanBeNull]
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            nestedFontStores.ForEach(f => f.Dispose());
            glyphStores.ForEach(g => g.Dispose());
        }
    }
}
