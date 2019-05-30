// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace osu.Framework.IO.Stores
{
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
            cachedTextureLookup = t => Get(getTextureName(t.Item1, t.Item2));
        }

        /// <summary>
        /// Get the texture of a character from a specified font and its associated spacing information.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look for the character in.</param>
        /// <returns>A struct containing the texture and its associated spacing information for the specified character. Null if no texture is found.</returns>
        public CharacterGlyph? GetCharacter(string fontName, char charName)
        {
            var texture = namespacedTextureCache.GetOrAdd((fontName, charName), cachedTextureLookup);

            if (texture == null)
                return null;

            if (!getCharacterInfo(fontName, charName, out var info))
                return null;

            info.Texture = texture;
            info.ApplyScaleAdjust(1 / ScaleAdjust);

            return info;
        }

        /// <summary>
        /// Get the base height of a font containing a particiular character.
        /// </summary>
        /// <param name="c">The charcter to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(char c)
        {
            var glyphStore = getGlyphStore("", c);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Get the base height of a font containing a particiular character.
        /// </summary>
        /// <param name="fontName">The font to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(string fontName)
        {
            var glyphStore = getGlyphStore(fontName);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Looks for and gets the Character information from this store's <see cref="GlyphStore"/>s and nested <see cref="FontStore"/>s.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look in for the character.</param>
        /// <param name="glyph">The found glyph.</param>
        /// <returns>Whether a matching <see cref="CharacterGlyph"/> was found.</returns>
        private bool getCharacterInfo(string fontName, char charName, out CharacterGlyph glyph)
        {
            // Return the default (first available) character if fontName is default
            var glyphStore = getGlyphStore(fontName, charName);

            if (glyphStore == null)
            {
                glyph = default;
                return false;
            }

            glyph = glyphStore.GetCharacterInfo(charName);
            return true;
        }

        private string getTextureName(string fontName, char charName) => string.IsNullOrEmpty(fontName) ? charName.ToString() : fontName + "/" + charName;

        /// <summary>
        /// Performs a lookup of this FontStore's <see cref="GlyphStore"/>s and nested <see cref="FontStore"/>s for a GlyphStore that matches the provided condition.
        /// </summary>
        /// <param name="fontName">The font to look up the <see cref="GlyphStore"/> for.</param>
        /// <param name="charName">A character to look up in the <see cref="GlyphStore"/>.</param>
        /// <returns>The first available <see cref="GlyphStore"/> matches the name and contains the specified character. Null if not available.</returns>
        private GlyphStore getGlyphStore(string fontName, char? charName = null)
        {
            foreach (var store in glyphStores)
            {
                if (charName == null)
                {
                    if (store.FontName == fontName)
                        return store;
                }
                else
                {
                    if (store.ContainsTexture(getTextureName(fontName, charName.Value)))
                        return store;
                }
            }

            foreach (var store in nestedFontStores)
            {
                var nestedStore = store.getGlyphStore(fontName, charName);
                if (nestedStore != null)
                    return nestedStore;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            glyphStores.ForEach(g => g.Dispose());
        }

        /// <summary>
        /// Contains the texture and associated spacing information for a Character.
        /// </summary>
        public struct CharacterGlyph
        {
            /// <summary>
            /// The texture for this character.
            /// </summary>
            public Texture Texture { get; set; }

            /// <summary>
            /// The amount of space that should be given to the left of the character texture.
            /// </summary>
            public float XOffset { get; set; }

            /// <summary>
            /// The amount of space that should be given to the top of the character texture.
            /// </summary>
            public float YOffset { get; set; }

            /// <summary>
            /// The amount of space to advance the cursor by after drawing the texture.
            /// </summary>
            public float XAdvance { get; set; }

            /// <summary>
            /// The scale-adjusted width of the texture associated with this character.
            /// </summary>
            public float Width => Texture.DisplayWidth;

            /// <summary>
            /// The scale-adjusted height of the texture associated with this character.
            /// </summary>
            public float Height => Texture.DisplayHeight;

            public CharacterGlyph([CanBeNull] Texture texture = null, float xOffset = 0, float yOffset = 0, float xAdvance = 0)
            {
                Texture = texture;
                XOffset = xOffset;
                YOffset = yOffset;
                XAdvance = xAdvance;
            }

            /// <summary>
            /// Apply a scale adjust to metrics of this glyph.
            /// </summary>
            /// <param name="scaleAdjust">The adjustment to multiply all metrics by.</param>
            public void ApplyScaleAdjust(float scaleAdjust)
            {
                XOffset *= scaleAdjust;
                YOffset *= scaleAdjust;
                XAdvance *= scaleAdjust;
            }
        }
    }
}
