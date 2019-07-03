// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<TextureUpload>
    {
        private readonly Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private readonly All filteringMode;
        private readonly bool manualMipmaps;

        protected TextureAtlas Atlas;

        private const int max_atlas_size = 1024;

        /// <summary>
        /// Decides at what resolution multiple this <see cref="TextureStore"/> is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public readonly float ScaleAdjust;

        public TextureStore(IResourceStore<TextureUpload> store = null, bool useAtlas = true, All filteringMode = All.Linear, bool manualMipmaps = false, float scaleAdjust = 2)
            : base(store)
        {
            this.filteringMode = filteringMode;
            this.manualMipmaps = manualMipmaps;

            ScaleAdjust = scaleAdjust;

            AddExtension(@"png");
            AddExtension(@"jpg");

            if (useAtlas)
            {
                int size = Math.Min(max_atlas_size, GLWrapper.MaxTextureSize);
                Atlas = new TextureAtlas(size, size, filteringMode: filteringMode);
            }
        }

        private async Task<Texture> getTextureAsync(string name) => loadRaw(await base.GetAsync(name));

        private Texture getTexture(string name) => loadRaw(base.Get(name));

        private Texture loadRaw(TextureUpload upload)
        {
            if (upload == null) return null;

            TextureGL glTexture = null;

            if (Atlas != null)
            {
                if ((glTexture = Atlas.Add(upload.Width, upload.Height)) == null)
                    Logger.Log($"Texture requested ({upload.Width}x{upload.Height}) which exceeds {nameof(TextureStore)}'s atlas size ({max_atlas_size}x{max_atlas_size}) - bypassing atlasing. Consider using {nameof(LargeTextureStore)}.", LoggingTarget.Performance);
            }

            if (glTexture == null)
                glTexture = new TextureGLSingle(upload.Width, upload.Height, manualMipmaps, filteringMode);

            Texture tex = new Texture(glTexture) { ScaleAdjust = ScaleAdjust };

            tex.SetData(upload);

            return tex;
        }

        public new Task<Texture> GetAsync(string name) => Task.Run(() => Get(name)); // TODO: best effort. need to re-think textureCache data structure to fix this.

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            this.LogIfNonBackgroundThread(name);

            lock (textureCache)
            {
                // refresh the texture if no longer available (may have been previously disposed).
                if (!textureCache.TryGetValue(name, out var tex) || tex?.Available == false)
                {
                    try
                    {
                        textureCache[name] = tex = getTexture(name);
                    }
                    catch (TextureTooLargeForGLException)
                    {
                        Logger.Log($"Texture \"{name}\" exceeds the maximum size supported by this device ({GLWrapper.MaxTextureSize}px).", level: LogLevel.Error);
                    }
                }

                return tex;
            }
        }
    }
}
