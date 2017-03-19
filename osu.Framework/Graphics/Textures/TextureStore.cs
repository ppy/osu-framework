// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using System.Drawing;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System;
using System.Threading;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<RawTexture>
    {
        private readonly ConcurrentDictionary<string, Lazy<TextureGL>> textureCache = new ConcurrentDictionary<string, Lazy<TextureGL>>();

        private TextureAtlas atlas;

        /// <summary>
        /// Decides at what resolution multiple this texturestore is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public float ScaleAdjust = 2;

        public TextureStore(IResourceStore<RawTexture> store = null, bool useAtlas = true) : base(store)
        {
            AddExtension(@"png");
            AddExtension(@"jpg");

            if (useAtlas)
                atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize);
        }

        private Texture getTexture(string name)
        {
            RawTexture raw = base.Get($@"{name}");
            if (raw == null) return null;

            Texture tex = atlas != null ? atlas.Add(raw.Width, raw.Height) : new Texture(raw.Width, raw.Height);
            tex.SetData(new TextureUpload(raw.Pixels)
            {
                Bounds = new Rectangle(0, 0, raw.Width, raw.Height),
                Format = raw.PixelFormat,
            });

            return tex;
        }

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture Get(string name)
        {
            var cachedTex = textureCache.GetOrAdd(name, n =>
                //Laziness ensure we are only ever creating the texture once (and blocking on other access until it is done).
                new Lazy<TextureGL>(() => getTexture(name)?.TextureGL, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

            if (cachedTex == null) return null;

            //use existing TextureGL (but provide a new texture instance).
            var tex = new Texture(cachedTex)
            {
                ScaleAdjust = ScaleAdjust
            };

            return tex;
        }
    }
}