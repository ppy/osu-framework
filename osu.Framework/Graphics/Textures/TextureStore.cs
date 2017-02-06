// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<RawTexture>
    {
        Dictionary<string, TextureGL> textureCache = new Dictionary<string, TextureGL>();

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
            //don't allow concurrent texture retrievals for the time being.
            //can potentially make this happen if it ever becomes a thing we actually want.
            lock (textureCache)
            {
                Texture tex = null;

                try
                {
                    TextureGL cachedTex;
                    if (textureCache.TryGetValue(name, out cachedTex))
                    {
                        //use existing TextureGL (but provide a new texture instance).
                        return tex = cachedTex != null ? new Texture(cachedTex) : null;
                    }

                    tex = getTexture(name);

                    //load available mipmaps
                    //int level = 1;
                    //int div = 2;

                    //while (tex.Width / div > 0)
                    //{
                    //    s = base.GetStream($@"{name}/{div}");

                    //    if (s == null) break;

                    //    int w = tex.Width / div;
                    //    int h = tex.Height / div;

                    //    TextureUpload upload = new TextureUpload(w * h * 4)
                    //    {
                    //        Level = level
                    //    };

                    //    using (MagickImage image = new MagickImage(s))
                    //    {
                    //        if (image.Width != w || image.Height != h)
                    //        {
                    //            image.Resize(new MagickGeometry($"{w}x{h}!"));
                    //        }

                    //        image.Write(new MemoryStream(upload.Data), MagickFormat.Rgba);
                    //    }

                    //    tex.SetData(upload);

                    //    level++;
                    //    div *= 2;
                    //}

                    textureCache[name] = tex?.TextureGL;

                    return tex;
                }
                finally
                {
                    if (tex != null && ScaleAdjust != 1)
                        tex.ScaleAdjust = ScaleAdjust;
                }
            }
        }
    }
}
