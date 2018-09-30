// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<TextureUpload>
    {
        private readonly Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private readonly All filteringMode;
        private readonly bool manualMipmaps;
        private readonly TextureAtlas atlas;

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
                atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, filteringMode: filteringMode);
        }

        private async Task<Texture> getTextureAsync(string name) => loadRaw(await base.GetAsync(name));

        private Texture getTexture(string name) => loadRaw(base.Get(name));

        private Texture loadRaw(TextureUpload upload)
        {
            if (upload == null) return null;

            var glTexture = atlas != null ? atlas.Add(upload.Width, upload.Height) : new TextureGLSingle(upload.Width, upload.Height, manualMipmaps, filteringMode);

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

            lock (textureCache)
            {
                // refresh the texture if no longer available (may have been previously disposed).
                if (!textureCache.TryGetValue(name, out var tex) || tex?.Available == false)
                    textureCache[name] = tex = getTexture(name);

                return tex;
            }
        }
    }
}
