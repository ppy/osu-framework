// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<RawTexture>
    {
        private readonly ConcurrentDictionary<string, Lazy<TextureGL>> textureCache = new ConcurrentDictionary<string, Lazy<TextureGL>>();

        private readonly All filteringMode;
        private readonly bool manualMipmaps;
        private readonly TextureAtlas atlas;

        /// <summary>
        /// Decides at what resolution multiple this texturestore is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public float ScaleAdjust = 2;

        public TextureStore(IResourceStore<RawTexture> store = null, bool useAtlas = true, All filteringMode = All.Linear, bool manualMipmaps = false)
            : base(store)
        {
            this.filteringMode = filteringMode;
            this.manualMipmaps = manualMipmaps;
            AddExtension(@"png");
            AddExtension(@"jpg");

            if (useAtlas)
                atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, filteringMode: filteringMode);
        }

        private async Task<Texture> getTextureAsync(string name) => loadRaw(await base.GetAsync(name));

        private Texture getTexture(string name) => loadRaw(base.Get(name));

        private Texture loadRaw(RawTexture raw)
        {
            if (raw == null) return null;

            Texture tex = atlas != null ? atlas.Add(raw.Width, raw.Height) : new Texture(raw.Width, raw.Height, manualMipmaps, filteringMode);
            tex.SetData(new TextureUpload(raw));

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

            var cachedTex = textureCache.GetOrAdd(name, n =>
                //Laziness ensure we are only ever creating the texture once (and blocking on other access until it is done).
                new Lazy<TextureGL>(() => getTexture(name)?.TextureGL, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

            //use existing TextureGL (but provide a new texture instance).
            return cachedTex == null ? null : new Texture(cachedTex) { ScaleAdjust = ScaleAdjust };
        }
    }
}
