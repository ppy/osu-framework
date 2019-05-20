// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;

namespace osu.Framework.IO.Stores
{
    public struct CharacterGlyph
    {
        public Texture Texture;

        /// <summary>
        /// The amount of space that should be given to the left of the character texture
        /// </summary>
        public int XOffset;

        /// <summary>
        /// The amount of space that should be given to the top of the character texture
        /// </summary>
        public int YOffset;

        /// <summary>
        /// The amount of space to advance the cursor by after drawing the texture
        /// </summary>
        public int XAdvance;
    }

    public class FontStore : TextureStore
    {
        private readonly List<GlyphStore> glyphStores = new List<GlyphStore>();

        private readonly List<FontStore> nestedFontStores = new List<FontStore>();

        private readonly Func<(string, char), Texture> cachedTextureLookup;

        /// <summary>
        /// A local cache to avoid string allocation overhead. Can be changed to (string,char)=>string if this ever becomes an issue,
        /// but as long as we directly inherit <see cref="TextureStore"/> this is a slight optimisation.
        /// </summary>
        private readonly ConcurrentDictionary<(string, char), Texture> namespacedTextureCache = new ConcurrentDictionary<(string, char), Texture>();

        public FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100)
            : base(store, scaleAdjust: scaleAdjust)
        {
            cachedTextureLookup = t => string.IsNullOrEmpty(t.Item1) ? Get(t.Item2.ToString()) : Get(t.Item1 + "/" + t.Item2);
        }

        /// <summary>
        /// Get the texture of a character from a specified font and its associated spacing information.
        /// </summary>
        /// <param name="charName">The character to look up</param>
        /// <param name="fontName">The font look for the character in</param>
        /// <returns>The texture and the spacing information associated with the character and font. Returns null if no texture is found</returns>
        public CharacterGlyph? GetCharacter(string fontName, char charName)
        {
            var texture = namespacedTextureCache.GetOrAdd((fontName, charName), cachedTextureLookup);

            if (texture == null)
                return null;

            var info = getGlyphInfo(fontName, charName) ?? new CharacterGlyph();

            info.Texture = texture;
            return info;
        }

        /// <summary>
        /// Gets the spacing information for a character in a specified font
        /// </summary>
        /// <param name="charName">The character to look up</param>
        /// <param name="fontName">The font look for the character in</param>
        /// <returns>The associated spacing information for the character and font. Returns null if not found</returns>
        private CharacterGlyph? getGlyphInfo(string fontName, char charName)
        {
            foreach (var store in glyphStores)
            {
                // Return the default (first available) glyph if fontName is default
                if (store.HasGlyph(charName) && (fontName == store.FontName || fontName == ""))
                    return store.GetGlyphInfo(charName);
            }

            foreach (var store in nestedFontStores)
            {
                var glyph = store.getGlyphInfo(fontName, charName);
                if (glyph != null)
                    return glyph;
            }

            return null;
        }

        protected override IEnumerable<string> GetFilenames(string name)
        {
            // extensions should not be used as they interfere with character lookup.
            yield return name;
        }

        public override void AddStore(IResourceStore<TextureUpload> store)
        {
            switch (store)
            {
                case FontStore fs:
                    nestedFontStores.Add(fs);
                    return;
                case GlyphStore gs:
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
