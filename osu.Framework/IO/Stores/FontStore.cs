// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
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
            cachedTextureLookup = t =>
            {
                var tex = Get(getTextureName(t.Item1, t.Item2));

                if (tex == null)
                    Logger.Log($"Glyph texture lookup for {getTextureName(t.Item1, t.Item2)} was unsuccessful.");

                return tex;
            };
        }

        /// <summary>
        /// Attempts to retrieve the texture of a character from a specified font and its associated spacing information.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look for the character in.</param>
        /// <param name="glyph">The glyph retrieved, if it exists.</param>
        /// <returns>Whether or not a <see cref="CharacterGlyph"/> was able to be retrieved.</returns>
        public bool TryGetCharacter(string fontName, char charName, out CharacterGlyph glyph)
        {
            var texture = namespacedTextureCache.GetOrAdd((fontName, charName), cachedTextureLookup);

            if (texture == null)
            {
                glyph = default;
                return false;
            }

            Trace.Assert(tryGetCharacterGlyph(fontName, charName, out glyph));

            glyph.Texture = texture;
            glyph.ApplyScaleAdjust(1 / ScaleAdjust);

            return true;
        }

        /// <summary>
        /// Retrieves the base height of a font containing a particular character.
        /// </summary>
        /// <param name="c">The charcter to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(char c)
        {
            var glyphStore = getGlyphStore(string.Empty, c);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Retrieves the base height of a font containing a particular character.
        /// </summary>
        /// <param name="fontName">The font to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(string fontName)
        {
            var glyphStore = getGlyphStore(fontName);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Retrieves the character information from this <see cref="FontStore"/>.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look in for the character.</param>
        /// <param name="glyph">The found glyph.</param>
        /// <returns>Whether a matching <see cref="CharacterGlyph"/> was found. If a font name is not provided, gets the glyph from the first font store that supports it.</returns>
        private bool tryGetCharacterGlyph(string fontName, char charName, out CharacterGlyph glyph)
        {
            var glyphStore = getGlyphStore(fontName, charName);

            if (glyphStore == null)
            {
                glyph = default;
                return false;
            }

            glyph = glyphStore.GetCharacterInfo(charName);
            return true;
        }

        private string getTextureName(string fontName, char charName) => string.IsNullOrEmpty(fontName) ? charName.ToString() : $"{fontName}/{charName}";

        /// <summary>
        /// Retrieves a <see cref="GlyphStore"/> from this <see cref="FontStore"/> that matches a font and character.
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
        /// Contains the texture and associated spacing information for a character.
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

            /// <summary>
            /// Gets the kerning value for the previous character along with the one for this glyph, adjusted for the scale of the <see cref="FontStore"/>.
            /// <remarks>The kerning value is a unique spacing adjustment specified for each character pair by the font.</remarks>
            /// </summary>
            /// <param name="previous">The character previous to this one.</param>
            /// <returns>The scale-adjusted kerning value for the character pair</returns>
            public float GetKerningPair(char previous) => containingStore.GetKerningValue(previous, character) * scaleAdjust;

            private float scaleAdjust;

            private readonly GlyphStore containingStore;

            private readonly char character;

            public CharacterGlyph(char character, [CanBeNull] Texture texture = null, float xOffset = 0, float yOffset = 0, float xAdvance = 0, GlyphStore containingStore = null)
            {
                Texture = texture;
                XOffset = xOffset;
                YOffset = yOffset;
                XAdvance = xAdvance;

                scaleAdjust = 1;
                this.containingStore = containingStore;
                this.character = character;
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
                this.scaleAdjust = scaleAdjust;
            }
        }
    }
}
